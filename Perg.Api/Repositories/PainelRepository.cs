using Perg.Api.Contexts;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class PainelRepository : IPainelRepository
    {
        private readonly PergDbContext _context;

        public PainelRepository(PergDbContext context)
        {
            _context = context;
        }

        public int ContarPedidosDesde(DateTime desdeUtc) => _context.Pedidos.Count(p => p.CriadoEm >= desdeUtc);
        public bool ExistePedido() => _context.Pedidos.Any();
        public bool ExisteOrigem() => _context.Origens.Any();
        public bool ExisteFavorito() => _context.EmpresaFavoritos.Any();
    }
}
