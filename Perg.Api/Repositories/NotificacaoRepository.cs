using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    // Cada usuário só vê as próprias notificações (além do filtro por empresa do contexto)
    public class NotificacaoRepository : INotificacaoRepository
    {
        private readonly PergDbContext _context;

        public NotificacaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public (List<Notificacao> Itens, int Total) Listar(Guid usuarioId, bool somenteNaoLidas, int pagina, int tamanhoPagina)
        {
            IQueryable<Notificacao> consulta = _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UsuarioId == usuarioId);

            if (somenteNaoLidas)
            {
                consulta = consulta.Where(n => n.LidaEm == null);
            }

            int total = consulta.Count();

            List<Notificacao> itens = consulta
                .OrderByDescending(n => n.CriadoEm)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToList();

            return (itens, total);
        }

        // Usa o índice ix_notificacoes_nao_lidas (usuario_id, criado_em) WHERE lida_em IS NULL
        public int ContarNaoLidas(Guid usuarioId)
        {
            return _context.Notificacoes.Count(n => n.UsuarioId == usuarioId && n.LidaEm == null);
        }

        public Notificacao? ObterPorId(Guid id, Guid usuarioId)
        {
            return _context.Notificacoes.FirstOrDefault(n => n.Id == id && n.UsuarioId == usuarioId);
        }

        // Um único UPDATE no banco, sem carregar as notificações
        public int MarcarTodasComoLidas(Guid usuarioId, DateTime lidaEm)
        {
            return _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId && n.LidaEm == null)
                .ExecuteUpdate(s => s.SetProperty(n => n.LidaEm, (DateTime?)lidaEm));
        }

        public Usuario? ObterUsuario(Guid usuarioId)
        {
            return _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
