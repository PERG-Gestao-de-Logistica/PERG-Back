using Microsoft.EntityFrameworkCore;
using Perg.Api.Applications.Integracoes;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class IntegracaoRepository : IIntegracaoRepository
    {
        private readonly PergDbContext _context;

        public IntegracaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public ChaveApi? ObterChaveAtivaPorHash(string hash)
        {
            return _context.ChavesApi.IgnoreQueryFilters().FirstOrDefault(c => c.HashChave == hash && c.RevogadaEm == null);
        }

        // Grava o último uso no máximo a cada 5 minutos (não escreve no banco a cada chamada)
        public void RegistrarUsoDaChave(ChaveApi chave)
        {
            if (chave.UltimoUsoEm == null || DateTime.UtcNow - chave.UltimoUsoEm > TimeSpan.FromMinutes(5))
            {
                chave.UltimoUsoEm = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public List<ChaveApi> ListarChaves() =>
            _context.ChavesApi.AsNoTracking().OrderBy(c => c.RevogadaEm != null).ThenByDescending(c => c.CriadoEm).ToList();

        public ChaveApi? ObterChave(Guid id) => _context.ChavesApi.FirstOrDefault(c => c.Id == id);

        public void AdicionarChave(ChaveApi chave)
        {
            _context.ChavesApi.Add(chave);
            _context.SaveChanges();
        }

        public List<Webhook> ListarWebhooks() => _context.Webhooks.AsNoTracking().OrderBy(w => w.CriadoEm).ToList();

        public Webhook? ObterWebhook(Guid id) => _context.Webhooks.FirstOrDefault(w => w.Id == id);

        public void AdicionarWebhook(Webhook webhook)
        {
            _context.Webhooks.Add(webhook);
            _context.SaveChanges();
        }

        public void RemoverWebhook(Webhook webhook)
        {
            _context.Webhooks.Remove(webhook);
            _context.SaveChanges();
        }

        public List<WebhookEntrega> ListarEntregas(Guid webhookId, int quantidade) =>
            _context.WebhookEntregas.AsNoTracking().Where(e => e.WebhookId == webhookId)
                .OrderByDescending(e => e.CriadoEm).Take(quantidade).ToList();

        public List<Webhook> ListarWebhooksAtivosDaEmpresa(Guid empresaId) =>
            _context.Webhooks.IgnoreQueryFilters().Where(w => w.EmpresaId == empresaId && w.Ativo).ToList();

        public void AdicionarEntregas(IEnumerable<WebhookEntrega> entregas)
        {
            _context.WebhookEntregas.AddRange(entregas);
            _context.SaveChanges();
        }

        public List<WebhookEntrega> ListarEntregasParaEnviar(int quantidade)
        {
            DateTime agora = DateTime.UtcNow;
            return _context.WebhookEntregas
                .IgnoreQueryFilters()
                .Include(e => e.Webhook)
                .Where(e => e.Status == StatusWebhookEntrega.Pendente && e.ProximaTentativaEm <= agora)
                .OrderBy(e => e.ProximaTentativaEm)
                .Take(quantidade)
                .ToList();
        }

        public void Salvar() => _context.SaveChanges();
    }
}
