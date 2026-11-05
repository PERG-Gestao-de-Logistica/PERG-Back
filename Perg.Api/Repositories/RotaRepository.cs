using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class RotaRepository : IRotaRepository
    {
        private readonly PergDbContext _context;

        public RotaRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Rota> ListarPorData(DateOnly dataOperacao)
        {
            return _context.Rotas
                .AsNoTracking()
                .Include(r => r.Entregador)
                .Include(r => r.Paradas.OrderBy(p => p.Sequencia))
                    .ThenInclude(p => p.Pedido)
                .Where(r => r.DataOperacao == dataOperacao)
                .OrderBy(r => r.Numero)
                .ToList();
        }

        public Rota? ObterPorId(Guid id)
        {
            return _context.Rotas
                .Include(r => r.Entregador)
                .Include(r => r.OfertasRota)
                .Include(r => r.Paradas.OrderBy(p => p.Sequencia))
                    .ThenInclude(p => p.Pedido)
                .FirstOrDefault(r => r.Id == id);
        }

        public void Atualizar(List<PedidoEvento> eventos, List<Outbox> mensagens)
        {
            _context.PedidoEventos.AddRange(eventos);
            _context.Outbox.AddRange(mensagens);
            _context.SaveChanges();
        }
    }
}
