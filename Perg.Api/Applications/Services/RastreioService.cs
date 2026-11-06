using Perg.Api.Applications.Execucao;
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

        public RastreioService(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public LerRastreioDto Obter(string token)
        {
            Pedido? pedido = _pedidoRepository.ObterPorToken(token);

            // Mesma resposta para token inexistente e expirado
            if (pedido == null || (pedido.TokenExpiraEm.HasValue && pedido.TokenExpiraEm.Value < DateTime.UtcNow))
            {
                throw new DomainException("Pedido não encontrado.");
            }

            return new LerRastreioDto
            {
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
