using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class AvaliacaoRepository : IAvaliacaoRepository
    {
        private readonly PergDbContext _context;

        public AvaliacaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public Rota? ObterRotaComParadas(Guid rotaId)
        {
            return _context.Rotas
                .Include(r => r.Entregador)
                .Include(r => r.Paradas)
                .FirstOrDefault(r => r.Id == rotaId);
        }

        public bool RotaJaAvaliada(Guid rotaId) => _context.Avaliacoes.IgnoreQueryFilters().Any(a => a.RotaId == rotaId);

        public void Adicionar(Avaliacao avaliacao)
        {
            _context.Avaliacoes.Add(avaliacao);
            _context.SaveChanges();
        }

        public List<Rota> ListarRotasSemAvaliacao(DateTime desdeUtc, int quantidade)
        {
            return _context.Rotas
                .AsNoTracking()
                .Include(r => r.Entregador)
                .Include(r => r.Paradas)
                .Where(r => r.Status == StatusRota.Concluida && r.EntregadorId != null
                    && r.ConcluidaEm >= desdeUtc && r.Avaliacao == null)
                .OrderByDescending(r => r.ConcluidaEm)
                .Take(quantidade)
                .ToList();
        }

        public List<Avaliacao> ListarDaEmpresa(int quantidade)
        {
            return _context.Avaliacoes
                .AsNoTracking()
                .Include(a => a.Rota)
                .Include(a => a.Entregador)
                .OrderByDescending(a => a.CriadoEm)
                .Take(quantidade)
                .ToList();
        }

        public List<Avaliacao> ListarDoEntregador(Guid entregadorId)
        {
            return _context.Avaliacoes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(a => a.Rota)
                .Include(a => a.Empresa)
                .Where(a => a.EntregadorId == entregadorId)
                .OrderByDescending(a => a.CriadoEm)
                .ToList();
        }
    }
}
