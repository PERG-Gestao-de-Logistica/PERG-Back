using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
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

        // incluirAtivas: também as rotas de outros dias que ainda estão aceitas ou em andamento (Monitoramento)
        public List<Rota> ListarPorData(DateOnly dataOperacao, bool incluirAtivas = false)
        {
            return _context.Rotas
                .AsNoTracking()
                .Include(r => r.Entregador)
                .Include(r => r.Veiculo)
                .Include(r => r.Paradas.OrderBy(p => p.Sequencia))
                    .ThenInclude(p => p.Pedido)
                .Where(r => r.DataOperacao == dataOperacao
                    || (incluirAtivas && (r.Status == StatusRota.Aceita || r.Status == StatusRota.EmAndamento)))
                .OrderBy(r => r.Numero)
                .ToList();
        }

        public Rota? ObterPorId(Guid id)
        {
            return _context.Rotas
                .Include(r => r.Entregador)
                .Include(r => r.Veiculo)
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
