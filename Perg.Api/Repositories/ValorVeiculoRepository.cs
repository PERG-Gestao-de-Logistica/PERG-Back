using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class ValorVeiculoRepository : IValorVeiculoRepository
    {
        private readonly PergDbContext _context;

        public ValorVeiculoRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<ValorVeiculoEmpresa> ListarDaEmpresa()
        {
            return _context.ValoresVeiculo.AsNoTracking().ToList();
        }

        public void Substituir(Guid empresaId, List<ValorVeiculoEmpresa> novos)
        {
            using var transacao = _context.Database.BeginTransaction();

            _context.ValoresVeiculo.Where(v => v.EmpresaId == empresaId).ExecuteDelete();
            _context.ValoresVeiculo.AddRange(novos);
            _context.SaveChanges();

            transacao.Commit();
        }
    }
}
