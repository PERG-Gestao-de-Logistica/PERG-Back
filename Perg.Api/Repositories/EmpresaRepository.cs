using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class EmpresaRepository : IEmpresaRepository
    {
        private readonly PergDbContext _context;

        public EmpresaRepository(PergDbContext context)
        {
            _context = context;
        }

        public bool CnpjExiste(string cnpj)
        {
            return _context.Empresas.Any(e => e.Cnpj == cnpj);
        }

        public Empresa? ObterPorId(Guid id)
        {
            return _context.Empresas.FirstOrDefault(e => e.Id == id);
        }

        public void Cadastrar(Empresa empresa, Usuario admin, Origem origem)
        {
            _context.Empresas.Add(empresa);
            _context.Usuarios.Add(admin);
            _context.Origens.Add(origem);
            _context.SaveChanges();
        }
    }
}
