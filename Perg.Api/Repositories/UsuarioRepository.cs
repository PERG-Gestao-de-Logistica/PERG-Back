using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly PergDbContext _context;

        public UsuarioRepository(PergDbContext context)
        {
            _context = context;
        }

        public Usuario? ObterPorEmail(string email)
        {
            // E-mail é único no sistema todo, então a busca ignora o filtro por empresa
            return _context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.Empresa)
                .FirstOrDefault(u => u.Email == email);
        }

        public Usuario? ObterPorId(Guid id)
        {
            return _context.Usuarios
                .Include(u => u.Empresa)
                .FirstOrDefault(u => u.Id == id);
        }

        public bool EmailExiste(string email)
        {
            return _context.Usuarios.IgnoreQueryFilters().Any(u => u.Email == email);
        }

        // O usuário veio deste mesmo contexto (já rastreado), então basta salvar
        public void Atualizar(Usuario usuario)
        {
            _context.SaveChanges();
        }
    }
}
