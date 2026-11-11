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

        public RastreioService(IPedidoRepository pedidoRepository, IPosicaoAtualCache posicoes)
        {
            _pedidoRepository = pedidoRepository;
            _posicoes = posicoes;
        }

        public LerRastreioDto Obter(string token)
        {
            Pedido? pedido = _pedidoRepository.ObterPorToken(token);

            // Mesma resposta para token inexistente e expirado
            if (pedido == null || (pedido.TokenExpiraEm.HasValue && pedido.TokenExpiraEm.Value < DateTime.UtcNow))
            {
                throw new DomainException("Pedido não encontrado.");
            }

            // Parada ativa deste pedido numa rota que já saiu
            Parada? parada = pedido.Paradas.FirstOrDefault(p =>
                (p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho)
                && p.Rota.Status == StatusRota.EmAndamento);

            int? entregasAntes = null;
            PosicaoEntregadorRastreioDto? posicao = null;

            if (parada != null)
            {
                entregasAntes = parada.Rota.Paradas.Count(p =>
                    (p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho) && p.Sequencia < parada.Sequencia);

                PosicaoAtual? atual = parada.Rota.EntregadorId.HasValue ? _posicoes.Obter(parada.Rota.EntregadorId.Value) : null;
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

            return new LerRastreioDto
            {
                EntregasAntes = entregasAntes,
                PosicaoEntregador = posicao,
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
    }
}
