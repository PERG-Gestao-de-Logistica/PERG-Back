using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class OcorrenciaRepository : IOcorrenciaRepository
    {
        private readonly PergDbContext _context;

        public OcorrenciaRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Ocorrencia> Listar(bool somenteAbertas)
        {
            IQueryable<Ocorrencia> consulta = _context.Ocorrencias
                .AsNoTracking()
                .Include(o => o.Entregador)
                .Include(o => o.Parada).ThenInclude(p => p.Pedido);

            if (somenteAbertas)
            {
                consulta = consulta.Where(o => o.Status == StatusOcorrencia.Aberta);
            }

            return consulta.OrderByDescending(o => o.RegistradaEm).Take(200).ToList();
        }

        public Ocorrencia? ObterPorId(Guid id)
        {
            return _context.Ocorrencias
                .Include(o => o.Entregador)
                .Include(o => o.Parada).ThenInclude(p => p.Pedido)
                .FirstOrDefault(o => o.Id == id);
        }

        public void Adicionar(object entidade)
        {
            _context.Add(entidade);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
