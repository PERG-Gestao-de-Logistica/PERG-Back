using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class RoteirizacaoRepository : IRoteirizacaoRepository
    {
        private readonly PergDbContext _context;

        public RoteirizacaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Pedido> ListarPedidosProntos(Guid origemId)
        {
            return _context.Pedidos
                .Where(p => p.OrigemId == origemId
                    && p.Status == StatusPedido.Novo
                    && p.GeocodificacaoStatus == StatusGeocodificacao.Ok
                    && p.Modalidade != ModalidadePedido.Retirada
                    && p.Latitude != null && p.Longitude != null)
                .OrderBy(p => p.NumeroPedido)
                .ToList();
        }

        public List<Pedido> ObterPedidos(List<Guid> ids)
        {
            return _context.Pedidos.Where(p => ids.Contains(p.Id)).ToList();
        }

        public short ProximoNumeroRota(DateOnly dataOperacao)
        {
            short? maior = _context.Rotas
                .Where(r => r.DataOperacao == dataOperacao)
                .Max(r => (short?)r.Numero);

            return (short)((maior ?? 0) + 1);
        }

        public Roteirizacao? ObterPorId(Guid id)
        {
            return _context.Roteirizacoes
                .Include(r => r.Rotas.OrderBy(rota => rota.Numero))
                    .ThenInclude(rota => rota.Paradas.OrderBy(p => p.Sequencia))
                        .ThenInclude(p => p.Pedido)
                .FirstOrDefault(r => r.Id == id);
        }

        public void Adicionar(Roteirizacao roteirizacao, List<Rota> rotas, List<PedidoEvento> eventos, List<Outbox> mensagens)
        {
            _context.Roteirizacoes.Add(roteirizacao);
            _context.Rotas.AddRange(rotas);
            _context.PedidoEventos.AddRange(eventos);
            _context.Outbox.AddRange(mensagens);
            // As paradas entram junto com as rotas (coleção rota.Paradas); os pedidos já estão rastreados
            _context.SaveChanges();
        }
    }
}
