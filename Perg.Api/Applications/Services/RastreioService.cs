using Perg.Api.Applications.Execucao;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.RastreioDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class RastreioService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPosicaoAtualCache _posicoes;
        private readonly IEntregadorRepository _entregadorRepository;

        public RastreioService(IPedidoRepository pedidoRepository, IPosicaoAtualCache posicoes, IEntregadorRepository entregadorRepository)
        {
            _pedidoRepository = pedidoRepository;
            _posicoes = posicoes;
            _entregadorRepository = entregadorRepository;
        }

        public LerRastreioDto Obter(string token)
        {
            Pedido? pedido = _pedidoRepository.ObterPorToken(token);

            // Mesma resposta para token inexistente e expirado
            if (pedido == null || (pedido.TokenExpiraEm.HasValue && pedido.TokenExpiraEm.Value < DateTime.UtcNow))
            {
                throw new DomainException("Pedido não encontrado.");
            }

            Parada? parada = pedido.Paradas.FirstOrDefault(p =>
                (p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho)
                && p.Rota.Status == StatusRota.EmAndamento);

            int? entregasAntes = null;
            PosicaoEntregadorRastreioDto? posicao = null;
            DateTime? chegadaPrevista = null;

            if (parada != null)
            {
                entregasAntes = parada.Rota.Paradas.Count(p =>
                    (p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho) && p.Sequencia < parada.Sequencia);

                PosicaoAtual? atual = parada.Rota.EntregadorId.HasValue ? _posicoes.Obter(parada.Rota.EntregadorId.Value) : null;
                chegadaPrevista = PrevisaoCorrigida(parada);

                if (entregasAntes == 0 && atual != null)
                {
                    posicao = new PosicaoEntregadorRastreioDto
                    {
                        Latitude = atual.Latitude,
                        Longitude = atual.Longitude,
                        AtualizadaEm = atual.RegistradaEm
                    };
                }
            }

            // Rota em andamento ou a que entregou o pedido
            Parada? paradaDoEntregador = parada ?? pedido.Paradas
                .Where(p => p.Status == StatusParada.Entregue && p.Rota.EntregadorId.HasValue)
                .OrderByDescending(p => p.ConcluidaEm)
                .FirstOrDefault();

            return new LerRastreioDto
            {
                EntregasAntes = entregasAntes,
                PosicaoEntregador = posicao,
                ChegadaPrevista = chegadaPrevista,
                Entregador = paradaDoEntregador != null ? MontarEntregador(paradaDoEntregador.Rota) : null,
                Logradouro = pedido.Logradouro,
                Numero = pedido.Numero,
                Complemento = pedido.Complemento,
                Latitude = pedido.Latitude,
                Longitude = pedido.Longitude,
                NumeroPedido = pedido.NumeroPedido,
                NomeLoja = pedido.Empresa.NomeFantasia,
                Status = pedido.Status,
                Modalidade = pedido.Modalidade,
                Bairro = pedido.Bairro,
                Cidade = pedido.Cidade,
                JanelaInicio = pedido.JanelaInicio,
                JanelaFim = pedido.JanelaFim,
                EntregueEm = pedido.EntregueEm,
                PinEntrega = pedido.Status is StatusPedido.Entregue or StatusPedido.Cancelado
                    ? null
                    : PinEntrega.Gerar(pedido.TokenRastreamento),
                Eventos = pedido.PedidoEventos
                    .OrderBy(e => e.OcorridoEm)
                    .Select(e => new LerRastreioEventoDto
                    {
                        Descricao = e.Descricao,
                        StatusNovo = e.StatusNovo,
                        OcorridoEm = e.OcorridoEm
                    }).ToList()
            };
        }

        // A roteirização calcula a chegada a partir da coleta prevista; se a rota saiu atrasada, empurra junto
        private static DateTime? PrevisaoCorrigida(Parada parada)
        {
            if (!parada.ChegadaPrevista.HasValue) return null;

            Rota rota = parada.Rota;
            TimeSpan atraso = rota.IniciadaEm.HasValue && rota.ColetaPrevistaEm.HasValue && rota.IniciadaEm > rota.ColetaPrevistaEm
                ? rota.IniciadaEm.Value - rota.ColetaPrevistaEm.Value
                : TimeSpan.Zero;

            return parada.ChegadaPrevista.Value + atraso;
        }

        private EntregadorRastreioDto? MontarEntregador(Rota rota)
        {
            if (!rota.EntregadorId.HasValue) return null;

            VwEntregadorRede? rede = _entregadorRepository.ObterNaRede(rota.EntregadorId.Value);
            if (rede == null) return null;

            return new EntregadorRastreioDto
            {
                Nome = NomeCurto(rede.Nome),
                VeiculoTipo = rede.VeiculoTipo,
                VeiculoModelo = rota.Veiculo?.Modelo,
                VeiculoPlaca = rota.Veiculo?.Placa ?? rede.VeiculoPlaca,
                NotaMedia = rede.NotaMedia,
                TotalEntregas = rede.TotalEntregas ?? 0
            };
        }

        // "Marcos Moto Teste" -> "Marcos T."
        private static string NomeCurto(string? nome)
        {
            string[] partes = (nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return string.Empty;
            return partes.Length == 1 ? partes[0] : $"{partes[0]} {partes[^1][0]}.";
        }
    }
}
