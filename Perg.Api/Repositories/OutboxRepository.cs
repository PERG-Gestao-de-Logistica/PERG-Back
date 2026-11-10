using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PergDbContext _context;

        public OutboxRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Outbox> ListarPendentes(int quantidade)
        {
            // Usa o índice parcial ix_outbox_pendentes (publicado_em IS NULL)
            return _context.Outbox
                .Where(o => o.PublicadoEm == null)
                .OrderBy(o => o.CriadoEm)
                .Take(quantidade)
                .ToList();
        }

        public bool JaProcessada(string consumidor, Guid mensagemId)
        {
            return _context.MensagensProcessadas.Any(m => m.Consumidor == consumidor && m.MensagemId == mensagemId);
        }

        public void RegistrarProcessada(string consumidor, Guid mensagemId)
        {
            _context.MensagensProcessadas.Add(new MensagemProcessada
            {
                Consumidor = consumidor,
                MensagemId = mensagemId,
                ProcessadaEm = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
