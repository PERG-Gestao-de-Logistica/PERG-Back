using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Perg.Api.Applications.Comum;
using Perg.Api.Applications.Execucao;
using Perg.Api.Applications.Services;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.DTOs.PosicoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Simulacao
{
    // Só para demonstração (Demo:SimulacaoHabilitada = true no servidor de demo).
    // Com a simulação ligada para uma loja, a cada poucos segundos:
    //  1. rota ofertada -> o entregador que recebeu a oferta e está mais perto aceita;
    //  2. rota aceita -> o entregador inicia (abre o expediente se precisar);
    //  3. rota em andamento -> o entregador anda pelo trajeto (Demo:FatorVelocidade vezes mais rápido)
    //     e, ao passar por cada parada, marca "a caminho" e conclui a entrega com o PIN.
    // Tudo passa pelos mesmos serviços do app (ofertas, execução, posições), então eventos, e-mails,
    // rastreio, carteira e SignalR funcionam como numa entrega de verdade.
    public class EstadoSimulacao
    {
        // Loja -> até quando a simulação fica ligada
        public ConcurrentDictionary<Guid, DateTime> Empresas { get; } = new();

        // Rota -> progresso no trajeto
        public ConcurrentDictionary<Guid, ProgressoRota> Rotas { get; } = new();

        public static readonly TimeSpan DuracaoMaxima = TimeSpan.FromHours(6);

        public bool EstaLigada(Guid empresaId) =>
            Empresas.TryGetValue(empresaId, out DateTime ate) && ate > DateTime.UtcNow;
    }

    public class ProgressoRota
    {
        public required List<(double Lat, double Lng)> Pontos { get; init; }
        public required double[] Acumulado { get; init; } // metros até cada ponto
        public required Dictionary<Guid, double> FracaoParada { get; init; } // parada -> 0..1 do trajeto
        public required DateTime InicioSimulado { get; init; }
        public required double DuracaoSimuladaS { get; init; }
        public double TotalM => Acumulado.Length == 0 ? 0 : Acumulado[^1];
    }

    public class SimuladorRotas : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(4);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EstadoSimulacao _estado;
        private readonly IConfiguration _config;
        private readonly ILogger<SimuladorRotas> _logger;

        public SimuladorRotas(IServiceScopeFactory scopeFactory, EstadoSimulacao estado, IConfiguration config, ILogger<SimuladorRotas> logger)
        {
            _scopeFactory = scopeFactory;
            _estado = estado;
            _config = config;
            _logger = logger;
        }

        private double Fator => Math.Max(1, _config.GetValue("Demo:FatorVelocidade", 10.0));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_config.GetValue("Demo:SimulacaoHabilitada", false))
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var (empresaId, ate) in _estado.Empresas)
                {
                    if (ate <= DateTime.UtcNow)
                    {
                        _estado.Empresas.TryRemove(empresaId, out _);
                        continue;
                    }

                    try
                    {
                        Rodar(empresaId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha na simulação da loja {EmpresaId}.", empresaId);
                    }
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }

        private void Rodar(Guid empresaId)
        {
            AceitarOfertadas(empresaId);
            IniciarAceitas(empresaId);
            AndarEmAndamento(empresaId);
        }

        // 1. Ofertada: aceita pelo entregador com oferta aberta mais perto do ponto de coleta
        private void AceitarOfertadas(Guid empresaId)
        {
            using IServiceScope escopo = _scopeFactory.CreateScope();
            PergDbContext contexto = escopo.ServiceProvider.GetRequiredService<PergDbContext>();
            IPosicaoAtualCache posicoes = escopo.ServiceProvider.GetRequiredService<IPosicaoAtualCache>();
            DateTime agora = DateTime.UtcNow;

            var rotas = contexto.Rotas.IgnoreQueryFilters().AsNoTracking()
                .Include(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                .Include(r => r.OfertasRota)
                .Where(r => r.EmpresaId == empresaId && r.Status == StatusRota.Ofertada)
                .ToList();

            foreach (Rota rota in rotas)
            {
                var abertas = rota.OfertasRota
                    .Where(o => o.Status == StatusOferta.Enviada && o.ExpiraEm > agora)
                    .OrderBy(o => DistanciaEntregador.KmAte(posicoes.Obter(o.EntregadorId), rota.Roteirizacao?.Origem) ?? double.MaxValue)
                    .ToList();

                foreach (OfertaRota oferta in abertas)
                {
                    using IServiceScope escopoAceite = _scopeFactory.CreateScope();
                    try
                    {
                        escopoAceite.ServiceProvider.GetRequiredService<OfertaService>().Aceitar(oferta.Id, oferta.EntregadorId);
                        _logger.LogInformation("Simulação: rota {RotaId} aceita por {EntregadorId}.", rota.Id, oferta.EntregadorId);
                        break;
                    }
                    catch (DomainException)
                    {
                        // Este entregador não pode (sem expediente, já com rota...): tenta o próximo
                    }
                }
            }
        }

        // 2. Aceita: o entregador inicia a rota
        private void IniciarAceitas(Guid empresaId)
        {
            List<(Guid RotaId, Guid EntregadorId)> aceitas;
            using (IServiceScope escopo = _scopeFactory.CreateScope())
            {
                aceitas = escopo.ServiceProvider.GetRequiredService<PergDbContext>().Rotas.IgnoreQueryFilters().AsNoTracking()
                    .Where(r => r.EmpresaId == empresaId && r.Status == StatusRota.Aceita && r.EntregadorId != null)
                    .Select(r => new { r.Id, EntregadorId = r.EntregadorId!.Value })
                    .AsEnumerable()
                    .Select(r => (r.Id, r.EntregadorId))
                    .ToList();
            }

            foreach (var (rotaId, entregadorId) in aceitas)
            {
                using IServiceScope escopo = _scopeFactory.CreateScope();
                ExecucaoService execucao = escopo.ServiceProvider.GetRequiredService<ExecucaoService>();
                try
                {
                    if (execucao.ObterJornada(entregadorId) == null)
                    {
                        execucao.IniciarJornada(entregadorId);
                    }
                    execucao.IniciarRota(rotaId, entregadorId);
                    _logger.LogInformation("Simulação: rota {RotaId} iniciada.", rotaId);
                }
                catch (DomainException ex)
                {
                    _logger.LogInformation("Simulação: não iniciou a rota {RotaId}: {Motivo}", rotaId, ex.Message);
                }
            }
        }

        // 3. Em andamento: anda pelo trajeto e conclui as paradas pelo caminho
        private void AndarEmAndamento(Guid empresaId)
        {
            List<Rota> rotas;
            using (IServiceScope escopo = _scopeFactory.CreateScope())
            {
                rotas = escopo.ServiceProvider.GetRequiredService<PergDbContext>().Rotas.IgnoreQueryFilters().AsNoTracking()
                    .Include(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                    .Include(r => r.Paradas).ThenInclude(p => p.Pedido)
                    .Where(r => r.EmpresaId == empresaId && r.Status == StatusRota.EmAndamento && r.EntregadorId != null)
                    .ToList();
            }

            HashSet<Guid> ativas = rotas.Select(r => r.Id).ToHashSet();
            foreach (Guid rotaId in _estado.Rotas.Keys.Where(id => !ativas.Contains(id)).ToList())
            {
                _estado.Rotas.TryRemove(rotaId, out _);
            }

            foreach (Rota rota in rotas)
            {
                ProgressoRota? progresso = _estado.Rotas.GetOrAdd(rota.Id, _ => MontarProgresso(rota)!);
                if (progresso == null || progresso.Pontos.Count == 0)
                {
                    _estado.Rotas.TryRemove(rota.Id, out _);
                    continue;
                }

                Guid entregadorId = rota.EntregadorId!.Value;
                double t = Math.Clamp((DateTime.UtcNow - progresso.InicioSimulado).TotalSeconds / progresso.DuracaoSimuladaS, 0, 1);

                EnviarPosicao(entregadorId, progresso, t);
                ConcluirParadasAte(rota, entregadorId, progresso, t);
            }
        }

        private ProgressoRota? MontarProgresso(Rota rota)
        {
            List<Parada> paradas = rota.Paradas
                .Where(p => p.Status != StatusParada.Cancelada && p.Pedido.Latitude != null && p.Pedido.Longitude != null)
                .OrderBy(p => p.Sequencia)
                .ToList();

            Origem? origem = rota.Roteirizacao?.Origem;
            List<(double Lat, double Lng)> pontos = Polyline.Decodificar(rota.Trajeto);
            if (pontos.Count < 2)
            {
                // Sem trajeto do OSRM: linha reta coleta -> paradas
                pontos = new List<(double, double)>();
                if (origem?.Latitude != null && origem.Longitude != null)
                {
                    pontos.Add(((double)origem.Latitude.Value, (double)origem.Longitude.Value));
                }
                pontos.AddRange(paradas.Select(p => ((double)p.Pedido.Latitude!.Value, (double)p.Pedido.Longitude!.Value)));
            }
            if (pontos.Count < 2)
            {
                return null;
            }

            var acumulado = new double[pontos.Count];
            for (int i = 1; i < pontos.Count; i++)
            {
                acumulado[i] = acumulado[i - 1] + Metros(pontos[i - 1], pontos[i]);
            }
            double total = Math.Max(1, acumulado[^1]);

            // Cada parada fica no ponto do trajeto mais perto dela, sempre depois da anterior
            var fracoes = new Dictionary<Guid, double>();
            int inicioBusca = 0;
            foreach (Parada parada in paradas)
            {
                var alvo = ((double)parada.Pedido.Latitude!.Value, (double)parada.Pedido.Longitude!.Value);
                int melhor = inicioBusca;
                double menor = double.MaxValue;
                for (int i = inicioBusca; i < pontos.Count; i++)
                {
                    double d = Metros(pontos[i], alvo);
                    if (d < menor) { menor = d; melhor = i; }
                }
                fracoes[parada.Id] = acumulado[melhor] / total;
                inicioBusca = melhor;
            }

            // Duração prevista (ou 25 km/h), acelerada; no mínimo 1 minuto
            double duracaoReal = rota.DuracaoPrevistaS is > 0 ? rota.DuracaoPrevistaS.Value : total / (25_000.0 / 3600);
            double duracao = Math.Max(60, duracaoReal / Fator);

            // Se a rota já tem entregas concluídas, começa da última concluída
            double jaFeito = paradas
                .Where(p => p.Status is StatusParada.Entregue or StatusParada.Falhou)
                .Select(p => fracoes[p.Id])
                .DefaultIfEmpty(0)
                .Max();

            return new ProgressoRota
            {
                Pontos = pontos,
                Acumulado = acumulado,
                FracaoParada = fracoes,
                DuracaoSimuladaS = duracao,
                InicioSimulado = DateTime.UtcNow.AddSeconds(-jaFeito * duracao)
            };
        }

        private void EnviarPosicao(Guid entregadorId, ProgressoRota progresso, double t)
        {
            double alvoM = t * progresso.TotalM;
            int i = Array.FindIndex(progresso.Acumulado, a => a >= alvoM);
            (double Lat, double Lng) ponto;
            if (i <= 0)
            {
                ponto = progresso.Pontos[0];
            }
            else
            {
                double trecho = progresso.Acumulado[i] - progresso.Acumulado[i - 1];
                double f = trecho <= 0 ? 0 : (alvoM - progresso.Acumulado[i - 1]) / trecho;
                var a = progresso.Pontos[i - 1];
                var b = progresso.Pontos[i];
                ponto = (a.Lat + (b.Lat - a.Lat) * f, a.Lng + (b.Lng - a.Lng) * f);
            }

            double velocidade = t >= 1 ? 0 : progresso.TotalM / progresso.DuracaoSimuladaS * 3.6 / Fator;

            using IServiceScope escopo = _scopeFactory.CreateScope();
            try
            {
                escopo.ServiceProvider.GetRequiredService<PosicaoService>().Registrar(entregadorId, new EnviarPosicoesDto
                {
                    Posicoes = new List<PosicaoDto>
                    {
                        new()
                        {
                            Latitude = Math.Round((decimal)ponto.Lat, 6),
                            Longitude = Math.Round((decimal)ponto.Lng, 6),
                            RegistradaEm = DateTimeOffset.UtcNow,
                            PrecisaoM = 8,
                            VelocidadeKmh = Math.Round((decimal)velocidade, 1)
                        }
                    }
                });
            }
            catch (DomainException ex)
            {
                _logger.LogInformation("Simulação: posição de {EntregadorId} não enviada: {Motivo}", entregadorId, ex.Message);
            }
        }

        private void ConcluirParadasAte(Rota rota, Guid entregadorId, ProgressoRota progresso, double t)
        {
            foreach (Parada parada in rota.Paradas.OrderBy(p => p.Sequencia))
            {
                if (parada.Status is not (StatusParada.Pendente or StatusParada.ACaminho)) continue;
                if (!progresso.FracaoParada.TryGetValue(parada.Id, out double fracao)) continue;

                using IServiceScope escopo = _scopeFactory.CreateScope();
                ExecucaoService execucao = escopo.ServiceProvider.GetRequiredService<ExecucaoService>();
                try
                {
                    if (parada.Status == StatusParada.Pendente)
                    {
                        execucao.MarcarACaminho(parada.Id, entregadorId);
                    }

                    if (t < fracao)
                    {
                        // Ainda chegando nesta parada: as próximas esperam
                        return;
                    }

                    var ponto = ((double)parada.Pedido.Latitude!.Value, (double)parada.Pedido.Longitude!.Value);
                    execucao.ConcluirEntrega(parada.Id, new ConcluirEntregaDto
                    {
                        TipoComprovante = TipoComprovante.Pin,
                        Pin = PinEntrega.Gerar(parada.Pedido.TokenRastreamento),
                        RecebedorNome = parada.Pedido.ClienteNome,
                        Latitude = (decimal)ponto.Item1,
                        Longitude = (decimal)ponto.Item2
                    }, entregadorId);
                    _logger.LogInformation("Simulação: pedido #{Numero} entregue.", parada.Pedido.NumeroPedido);
                }
                catch (DomainException ex)
                {
                    _logger.LogInformation("Simulação: parada {ParadaId} não concluída: {Motivo}", parada.Id, ex.Message);
                    return;
                }
            }
        }

        private static double Metros((double Lat, double Lng) a, (double Lat, double Lng) b)
        {
            const double R = 6_371_000;
            double ParaRad(double g) => g * Math.PI / 180;
            double dLat = ParaRad(b.Lat - a.Lat);
            double dLng = ParaRad(b.Lng - a.Lng);
            double h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(ParaRad(a.Lat)) * Math.Cos(ParaRad(b.Lat)) * Math.Pow(Math.Sin(dLng / 2), 2);
            return 2 * R * Math.Asin(Math.Sqrt(h));
        }
    }

    // Polyline do OSRM (Google Encoded Polyline, precisão 5)
    public static class Polyline
    {
        public static List<(double Lat, double Lng)> Decodificar(string? codificado)
        {
            var pontos = new List<(double, double)>();
            if (string.IsNullOrEmpty(codificado)) return pontos;

            int indice = 0, lat = 0, lng = 0;
            while (indice < codificado.Length)
            {
                lat += ProximoValor(codificado, ref indice);
                if (indice >= codificado.Length) break;
                lng += ProximoValor(codificado, ref indice);
                pontos.Add((lat / 1e5, lng / 1e5));
            }
            return pontos;
        }

        private static int ProximoValor(string s, ref int indice)
        {
            int resultado = 0, deslocamento = 0, b;
            do
            {
                if (indice >= s.Length) return 0;
                b = s[indice++] - 63;
                resultado |= (b & 0x1f) << deslocamento;
                deslocamento += 5;
            } while (b >= 0x20);
            return (resultado & 1) != 0 ? ~(resultado >> 1) : resultado >> 1;
        }
    }
}
