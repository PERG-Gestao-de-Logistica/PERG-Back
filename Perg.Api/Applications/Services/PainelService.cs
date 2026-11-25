using Perg.Api.Applications.Comum;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.PainelDto;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class PainelService
    {
        private static readonly TimeSpan ToleranciaPrazo = TimeSpan.FromMinutes(15);

        private readonly IPainelRepository _painelRepository;
        private readonly IRotaRepository _rotaRepository;
        private readonly IOcorrenciaRepository _ocorrenciaRepository;
        private readonly IEmpresaRepository _empresaRepository;

        public PainelService(IPainelRepository painelRepository, IRotaRepository rotaRepository,
            IOcorrenciaRepository ocorrenciaRepository, IEmpresaRepository empresaRepository)
        {
            _painelRepository = painelRepository;
            _rotaRepository = rotaRepository;
            _ocorrenciaRepository = ocorrenciaRepository;
            _empresaRepository = empresaRepository;
        }

        public ResumoPainelDto ObterResumo(Guid empresaId)
        {
            // "Hoje" é o dia no fuso da loja; o banco guarda em UTC
            Empresa? empresa = _empresaRepository.ObterPorId(empresaId);
            TimeZoneInfo fuso = TimeZoneInfo.FindSystemTimeZoneById(empresa?.FusoHorario ?? "America/Sao_Paulo");
            DateTime agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso);
            DateTime inicioDoDiaUtc = TimeZoneInfo.ConvertTimeToUtc(agoraLocal.Date, fuso);

            List<Rota> rotas = _rotaRepository.ListarPorData(DateOnly.FromDateTime(agoraLocal))
                .Where(r => r.Status != StatusRota.Cancelada)
                .ToList();

            List<Parada> paradasEntregues = rotas
                .SelectMany(r => r.Paradas)
                .Where(p => p.Status == StatusParada.Entregue && p.ConcluidaEm.HasValue && p.ChegadaPrevista.HasValue)
                .ToList();

            return new ResumoPainelDto
            {
                PrimeirosPassos = new PrimeirosPassosDto
                {
                    PontoColetaCadastrado = _painelRepository.ExisteOrigem(),
                    EntregadorFavorito = _painelRepository.ExisteFavorito(),
                    PedidoCriado = _painelRepository.ExistePedido()
                },
                PedidosHoje = _painelRepository.ContarPedidosDesde(inicioDoDiaUtc),
                RotasAtivas = rotas.Count(r => r.Status is StatusRota.Ofertada or StatusRota.Aceita or StatusRota.EmAndamento),
                RotasEmAndamento = rotas.Count(r => r.Status == StatusRota.EmAndamento),
                NoPrazoPercentual = paradasEntregues.Count == 0
                    ? null
                    : Math.Round(100m * paradasEntregues.Count(p => p.ConcluidaEm <= p.ChegadaPrevista + ToleranciaPrazo) / paradasEntregues.Count, 1),
                KmRodados = Math.Round(rotas.Sum(r => DistanciaPercorridaM(r)) / 1000m, 1),
                RotasHoje = rotas.Select(r => new RotaHojeDto
                {
                    Id = r.Id,
                    Codigo = Formatos.CodigoRota(r),
                    EntregadorNome = r.Entregador?.Nome,
                    Paradas = ParadasValidas(r).Count,
                    Regiao = r.Regiao,
                    TipoVeiculo = r.TipoVeiculo,
                    Status = r.Status,
                    Progresso = Progresso(r)
                }).ToList(),
                OcorrenciasAbertas = _ocorrenciaRepository.Listar(somenteAbertas: true)
                    .Take(5)
                    .Select(o => new OcorrenciaAbertaDto
                    {
                        Id = o.Id,
                        NumeroPedido = o.Parada.Pedido.NumeroPedido,
                        ClienteNome = o.Parada.Pedido.ClienteNome,
                        Tipo = o.Tipo,
                        EntregadorNome = o.Entregador?.Nome,
                        RegistradaEm = o.RegistradaEm
                    }).ToList()
            };
        }

        private static List<Parada> ParadasValidas(Rota rota) =>
            rota.Paradas.Where(p => p.Status != StatusParada.Cancelada).ToList();

        // Percentual de paradas finalizadas (entregues ou com falha)
        private static int Progresso(Rota rota)
        {
            if (rota.Status == StatusRota.Concluida) return 100;
            List<Parada> paradas = ParadasValidas(rota);
            if (paradas.Count == 0) return 0;
            int finalizadas = paradas.Count(p => p.Status is StatusParada.Entregue or StatusParada.Falhou);
            return (int)Math.Round(100.0 * finalizadas / paradas.Count);
        }

        // Concluída: distância real (ou a prevista). Em andamento: a prevista proporcional ao progresso.
        private static decimal DistanciaPercorridaM(Rota rota)
        {
            if (rota.Status == StatusRota.Concluida) return rota.DistanciaRealM ?? rota.DistanciaPrevistaM ?? 0;
            if (rota.Status == StatusRota.EmAndamento) return (rota.DistanciaPrevistaM ?? 0) * Progresso(rota) / 100m;
            return 0;
        }
    }
}
