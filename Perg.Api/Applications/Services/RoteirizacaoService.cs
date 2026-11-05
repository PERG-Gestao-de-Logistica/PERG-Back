using System.Text.Json;
using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.RoteirizacoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class RoteirizacaoService
    {
        private readonly IRoteirizacaoRepository _repository;
        private readonly IOrigemRepository _origemRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IMatrizDistancias _matrizDistancias;
        private readonly OtimizadorRotas _otimizador;
        private readonly IGeocodificador _geocodificador;

        public RoteirizacaoService(IRoteirizacaoRepository repository, IOrigemRepository origemRepository,
            IEmpresaRepository empresaRepository, IMatrizDistancias matrizDistancias, OtimizadorRotas otimizador,
            IGeocodificador geocodificador)
        {
            _geocodificador = geocodificador;
            _repository = repository;
            _origemRepository = origemRepository;
            _empresaRepository = empresaRepository;
            _matrizDistancias = matrizDistancias;
            _otimizador = otimizador;
        }

        public LerRoteirizacaoDto ObterPorId(Guid id)
        {
            Domains.Roteirizacao roteirizacao = _repository.ObterPorId(id)
                ?? throw new DomainException("Roteirização não encontrada.");

            return ParaDto(roteirizacao);
        }

        public LerRoteirizacaoDto Criar(CriarRoteirizacaoDto dto, Guid empresaId, Guid usuarioId)
        {
            Validar(dto);

            Origem origem = (dto.OrigemId.HasValue ? _origemRepository.ObterPorId(dto.OrigemId.Value) : _origemRepository.ObterPadrao())
                ?? throw new DomainException("Origem não encontrada.");

            // Origem cadastrada antes da geocodificação existir (ou quando o serviço estava fora): tenta agora.
            // A origem está rastreada pelo contexto, então as coordenadas são salvas junto com a roteirização.
            if (origem.Latitude == null || origem.Longitude == null)
            {
                ResultadoGeocodificacao local = _geocodificador.Geocodificar(new EnderecoGeocodificacao
                {
                    Logradouro = origem.Logradouro,
                    Numero = origem.Numero,
                    Bairro = origem.Bairro,
                    Cidade = origem.Cidade,
                    Uf = origem.Uf,
                    Cep = origem.Cep
                });
                origem.Latitude = local.Latitude;
                origem.Longitude = local.Longitude;
            }

            if (origem.Latitude == null || origem.Longitude == null)
            {
                throw new DomainException("Não foi possível localizar o endereço da origem. Confira o endereço da loja.");
            }

            Empresa empresa = _empresaRepository.ObterPorId(empresaId)
                ?? throw new DomainException("Empresa não encontrada.");
            TimeZoneInfo fuso = TimeZoneInfo.FindSystemTimeZoneById(empresa.FusoHorario);

            // ---------- 1. Quais pedidos entram ----------
            var foraDaRota = new List<PedidoForaDaRotaDto>();
            List<Pedido> pedidos = SelecionarPedidos(dto, origem, fuso, foraDaRota);

            // ---------- 2. Horário de saída e janelas ----------
            DateTime saida = CalcularSaida(dto.DataOperacao, origem, fuso);
            long jornadaS = (long)(dto.JornadaHoras * 3600);

            var entregas = new List<Pedido>();
            foreach (Pedido pedido in pedidos)
            {
                if (pedido.JanelaFim.HasValue && pedido.JanelaFim.Value <= saida)
                {
                    foraDaRota.Add(Fora(pedido, "A janela de entrega termina antes da saída da rota."));
                    continue;
                }
                entregas.Add(pedido);
            }

            if (entregas.Count == 0)
            {
                throw new DomainException("Nenhum pedido pronto para roteirizar (status novo e localização ok).");
            }

            // ---------- 3. Matriz de distâncias (nó 0 = loja) ----------
            var pontos = new List<PontoGeografico> { new(origem.Latitude.Value, origem.Longitude.Value) };
            pontos.AddRange(entregas.Select(p => new PontoGeografico(p.Latitude!.Value, p.Longitude!.Value)));
            MatrizDistancias matriz = _matrizDistancias.Calcular(pontos);

            // ---------- 4. Otimização (OR-Tools) ----------
            ProblemaRoteirizacao problema = MontarProblema(dto, entregas, matriz, saida, jornadaS);
            SolucaoRoteirizacao? solucao = _otimizador.Otimizar(problema);

            var roteirizacao = new Domains.Roteirizacao
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                OrigemId = origem.Id,
                SolicitadaPor = usuarioId,
                Objetivo = dto.Objetivo,
                Parametros = JsonSerializer.Serialize(dto),
                IniciadaEm = DateTime.UtcNow
            };

            var rotas = new List<Rota>();
            var eventos = new List<PedidoEvento>();
            var mensagens = new List<Outbox>();

            if (solucao == null || solucao.Rotas.Count == 0)
            {
                roteirizacao.Status = StatusRoteirizacao.Falhou;
                roteirizacao.Erro = "Não foi possível montar nenhuma rota. Confira a frota, a capacidade dos veículos e as janelas de entrega.";
                roteirizacao.ConcluidaEm = DateTime.UtcNow;
                _repository.Adicionar(roteirizacao, rotas, eventos, mensagens);
                return ObterPorId(roteirizacao.Id);
            }

            // ---------- 5. Grava rotas e paradas ----------
            short numero = _repository.ProximoNumeroRota(dto.DataOperacao);

            foreach (RotaOtimizada otimizada in solucao.Rotas)
            {
                VeiculoDisponivel veiculo = problema.Veiculos[otimizada.VeiculoIndice];

                var rota = new Rota
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = empresaId,
                    RoteirizacaoId = roteirizacao.Id,
                    Numero = numero,
                    TipoVeiculo = veiculo.Tipo,
                    Status = StatusRota.Proposta,
                    Cor = ParametrosVeiculo.Cores[(numero - 1) % ParametrosVeiculo.Cores.Length],
                    DataOperacao = dto.DataOperacao,
                    DistanciaPrevistaM = (int)otimizada.DistanciaM,
                    DuracaoPrevistaS = (int)otimizada.DuracaoS,
                    ColetaPrevistaEm = saida.AddSeconds(otimizada.SaidaS),
                    FimPrevistoEm = saida.AddSeconds(otimizada.SaidaS + otimizada.DuracaoS)
                };

                short sequencia = 1;
                foreach (ParadaOtimizada paradaOtimizada in otimizada.Paradas)
                {
                    Pedido pedido = entregas[paradaOtimizada.No - 1];

                    rota.Paradas.Add(new Parada
                    {
                        Id = Guid.CreateVersion7(),
                        EmpresaId = empresaId,
                        RotaId = rota.Id,
                        PedidoId = pedido.Id,
                        Sequencia = sequencia++,
                        Status = StatusParada.Pendente,
                        ChegadaPrevista = saida.AddSeconds(paradaOtimizada.ChegadaS)
                    });

                    pedido.Status = StatusPedido.Roteirizado;
                    string codigo = "R" + numero.ToString("00");
                    eventos.Add(CriarEvento(pedido, usuarioId, rota.Id, "PedidoRoteirizado", $"Pedido incluído na rota {codigo}"));
                    mensagens.Add(CriarMensagem("pedido", pedido.Id, "PedidoRoteirizado", new { pedidoId = pedido.Id, rotaId = rota.Id }));
                }

                // Região = bairro que mais aparece na rota
                rota.Regiao = otimizada.Paradas
                    .Select(p => entregas[p.No - 1].Bairro)
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .GroupBy(b => b)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                // Desenho da rota pelas ruas (opcional)
                var pontosRota = new List<PontoGeografico> { pontos[0] };
                pontosRota.AddRange(otimizada.Paradas.Select(p => pontos[p.No]));
                pontosRota.Add(pontos[0]);
                rota.Trajeto = _matrizDistancias.Trajeto(pontosRota);

                // Valor sugerido ao entregador (a loja pode mudar ao ofertar)
                rota.ValorEntregador = ParametrosVeiculo.CalcularValorEntregador(
                    rota.TipoVeiculo, rota.DistanciaPrevistaM ?? 0, rota.Paradas.Count);
                rota.CustoEstimado = rota.ValorEntregador;

                rotas.Add(rota);
                numero++;
            }

            foreach (int no in solucao.NosForaDaRota)
            {
                foraDaRota.Add(Fora(entregas[no - 1], "Não coube em nenhuma rota (capacidade, janela de entrega ou jornada)."));
            }

            var resumo = new ResultadoRoteirizacaoDto
            {
                QuantidadeRotas = rotas.Count,
                PedidosRoteirizados = rotas.Sum(r => r.Paradas.Count),
                KmTotal = Math.Round(rotas.Sum(r => r.DistanciaPrevistaM ?? 0) / 1000m, 1),
                DuracaoTotalS = rotas.Sum(r => (long)(r.DuracaoPrevistaS ?? 0)),
                RotaMaisLongaS = rotas.Max(r => (long)(r.DuracaoPrevistaS ?? 0)),
                DistanciasEstimadas = matriz.Estimada,
                PedidosForaDaRota = foraDaRota
            };

            roteirizacao.Status = StatusRoteirizacao.Concluida;
            roteirizacao.Resultado = JsonSerializer.Serialize(resumo);
            roteirizacao.ConcluidaEm = DateTime.UtcNow;

            mensagens.Add(CriarMensagem("roteirizacao", roteirizacao.Id, "RoteirizacaoConcluida",
                new { roteirizacaoId = roteirizacao.Id, empresaId, rotas = rotas.Select(r => r.Id) }));

            _repository.Adicionar(roteirizacao, rotas, eventos, mensagens);

            return ObterPorId(roteirizacao.Id);
        }

        // ---------- regras ----------

        private static void Validar(CriarRoteirizacaoDto dto)
        {
            if (dto.Frota == null || dto.Frota.Count == 0 || dto.Frota.Sum(f => f.Quantidade) <= 0)
            {
                throw new DomainException("Informe a frota disponível (tipo de veículo e quantidade).");
            }

            if (dto.Frota.Any(f => !Enum.IsDefined(f.Tipo) || f.Quantidade < 0
                || f.CapacidadeKg <= 0 || f.CapacidadeVolumes <= 0))
            {
                throw new DomainException("Frota inválida.");
            }

            if (dto.Frota.Sum(f => f.Quantidade) > 50)
            {
                throw new DomainException("Máximo de 50 veículos por roteirização.");
            }

            if (!Enum.IsDefined(dto.Objetivo))
            {
                throw new DomainException("Objetivo inválido.");
            }

            if (dto.JornadaHoras < 1 || dto.JornadaHoras > 14)
            {
                throw new DomainException("A jornada precisa ter entre 1 e 14 horas.");
            }

            if (dto.TempoParadaMinutos < 1 || dto.TempoParadaMinutos > 60)
            {
                throw new DomainException("O tempo por entrega precisa ficar entre 1 e 60 minutos.");
            }

            if (dto.EsperaMaximaMinutos < 15 || dto.EsperaMaximaMinutos > 480)
            {
                throw new DomainException("A espera máxima precisa ficar entre 15 e 480 minutos.");
            }

            if (dto.PedidoIds != null && dto.PedidoIds.Count > 99)
            {
                throw new DomainException("Máximo de 99 pedidos por roteirização.");
            }
        }

        private List<Pedido> SelecionarPedidos(CriarRoteirizacaoDto dto, Origem origem, TimeZoneInfo fuso, List<PedidoForaDaRotaDto> foraDaRota)
        {
            if (dto.PedidoIds == null || dto.PedidoIds.Count == 0)
            {
                // Mesmo dia + agendados cuja janela começa na data da operação
                return _repository.ListarPedidosProntos(origem.Id)
                    .Where(p => p.Modalidade == ModalidadePedido.MesmoDia
                        || (p.JanelaInicio.HasValue
                            && DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(p.JanelaInicio.Value, fuso)) == dto.DataOperacao))
                    .Take(99)
                    .ToList();
            }

            var escolhidos = new List<Pedido>();
            foreach (Pedido pedido in _repository.ObterPedidos(dto.PedidoIds.Distinct().ToList()))
            {
                string? motivo = pedido.Status != StatusPedido.Novo ? "O pedido não está com status novo."
                    : pedido.Modalidade == ModalidadePedido.Retirada ? "Pedido de retirada na loja."
                    : pedido.GeocodificacaoStatus != StatusGeocodificacao.Ok || pedido.Latitude == null
                        ? "A localização não está confirmada (geocodificação diferente de ok)."
                    : pedido.OrigemId != origem.Id ? "O pedido é de outra origem."
                    : null;

                if (motivo == null)
                {
                    escolhidos.Add(pedido);
                }
                else
                {
                    foraDaRota.Add(Fora(pedido, motivo));
                }
            }

            return escolhidos;
        }

        // Saída = abertura da loja no dia da operação; se for hoje e já passou, sai agora
        private static DateTime CalcularSaida(DateOnly data, Origem origem, TimeZoneInfo fuso)
        {
            DateTime abertura = TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(origem.Abre), fuso);
            DateTime agora = DateTime.UtcNow;

            if (abertura >= agora)
            {
                return abertura;
            }

            // Arredonda para os próximos 5 minutos
            DateTime saida = agora.AddMinutes(5 - agora.Minute % 5);
            return new DateTime(saida.Year, saida.Month, saida.Day, saida.Hour, saida.Minute, 0, DateTimeKind.Utc);
        }

        private static ProblemaRoteirizacao MontarProblema(CriarRoteirizacaoDto dto, List<Pedido> entregas,
            MatrizDistancias matriz, DateTime saida, long jornadaS)
        {
            int nos = entregas.Count + 1;
            var problema = new ProblemaRoteirizacao
            {
                Matriz = matriz,
                PesoGramas = new long[nos],
                Volumes = new long[nos],
                JanelaInicioS = new long[nos],
                JanelaFimS = new long[nos],
                TempoParadaS = dto.TempoParadaMinutos * 60L,
                JornadaMaximaS = jornadaS,
                Objetivo = dto.Objetivo
            };

            for (int i = 0; i < entregas.Count; i++)
            {
                Pedido pedido = entregas[i];
                int no = i + 1;

                problema.PesoGramas[no] = (long)Math.Ceiling(pedido.PesoKg * 1000);
                problema.Volumes[no] = pedido.Volumes;

                long inicio = pedido.JanelaInicio.HasValue ? (long)(pedido.JanelaInicio.Value - saida).TotalSeconds : 0;
                long fim = pedido.JanelaFim.HasValue ? (long)(pedido.JanelaFim.Value - saida).TotalSeconds : jornadaS;

                problema.JanelaInicioS[no] = Math.Clamp(inicio, 0, jornadaS);
                problema.JanelaFimS[no] = Math.Clamp(fim, problema.JanelaInicioS[no], jornadaS);
            }

            foreach (FrotaDto item in dto.Frota)
            {
                for (int i = 0; i < item.Quantidade; i++)
                {
                    problema.Veiculos.Add(new VeiculoDisponivel
                    {
                        Tipo = item.Tipo,
                        CapacidadeGramas = (long)((item.CapacidadeKg ?? ParametrosVeiculo.CapacidadeKg(item.Tipo)) * 1000),
                        CapacidadeVolumes = item.CapacidadeVolumes ?? ParametrosVeiculo.CapacidadeVolumes(item.Tipo)
                    });
                }
            }

            problema.EsperaMaximaS = dto.EsperaMaximaMinutos * 60L;

            // Mais pedidos = mais tempo de busca (10 a 30 segundos)
            problema.TempoLimiteSegundos = Math.Clamp(entregas.Count / 3, 10, 30);

            return problema;
        }

        private static PedidoForaDaRotaDto Fora(Pedido pedido, string motivo)
        {
            return new PedidoForaDaRotaDto { PedidoId = pedido.Id, NumeroPedido = pedido.NumeroPedido, Motivo = motivo };
        }

        private static PedidoEvento CriarEvento(Pedido pedido, Guid usuarioId, Guid rotaId, string tipo, string descricao)
        {
            return new PedidoEvento
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = pedido.EmpresaId,
                PedidoId = pedido.Id,
                RotaId = rotaId,
                TipoEvento = tipo,
                StatusNovo = StatusPedido.Roteirizado,
                Descricao = descricao,
                AutorTipo = AutorEvento.Usuario,
                AutorId = usuarioId,
                OcorridoEm = DateTime.UtcNow
            };
        }

        private static Outbox CriarMensagem(string topico, Guid chave, string tipoEvento, object payload)
        {
            return new Outbox
            {
                Id = Guid.CreateVersion7(),
                Topico = topico,
                Chave = chave.ToString(),
                TipoEvento = tipoEvento,
                Payload = JsonSerializer.Serialize(payload)
            };
        }

        private static LerRoteirizacaoDto ParaDto(Domains.Roteirizacao roteirizacao)
        {
            return new LerRoteirizacaoDto
            {
                Id = roteirizacao.Id,
                OrigemId = roteirizacao.OrigemId,
                Objetivo = roteirizacao.Objetivo,
                Status = roteirizacao.Status,
                Resultado = roteirizacao.Resultado == null ? null : JsonSerializer.Deserialize<ResultadoRoteirizacaoDto>(roteirizacao.Resultado),
                Erro = roteirizacao.Erro,
                CriadoEm = roteirizacao.CriadoEm,
                Rotas = roteirizacao.Rotas.OrderBy(r => r.Numero).Select(MapeadorRotas.ParaDto).ToList()
            };
        }
    }
}
