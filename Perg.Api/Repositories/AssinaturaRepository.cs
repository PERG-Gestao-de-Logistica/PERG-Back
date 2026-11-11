using Microsoft.EntityFrameworkCore;
using Perg.Api.Applications.Assinaturas;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class AssinaturaRepository : IAssinaturaRepository
    {
        private readonly PergDbContext _context;

        public AssinaturaRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Plano> ListarPlanosAtivos()
        {
            return _context.Planos.AsNoTracking().Where(p => p.Ativo).OrderBy(p => p.PrecoMensal).ToList();
        }

        public Plano? ObterPlano(Guid id)
        {
            return _context.Planos.FirstOrDefault(p => p.Id == id);
        }

        public Plano? ObterPlanoGratis()
        {
            return _context.Planos.Where(p => p.Ativo && p.PrecoMensal == 0).OrderBy(p => p.CriadoEm).FirstOrDefault();
        }

        public void GarantirPlanos(IEnumerable<Plano> padrao)
        {
            if (_context.Planos.Any()) return;
            _context.Planos.AddRange(padrao);
            _context.SaveChanges();
        }

        public Assinatura? ObterVigente(Guid empresaId)
        {
            return _context.Assinaturas
                .IgnoreQueryFilters()
                .Include(a => a.Plano)
                .FirstOrDefault(a => a.EmpresaId == empresaId && a.Status != StatusAssinatura.Cancelada);
        }

        public Assinatura? ObterPorIdNoGateway(string idNoGateway)
        {
            return _context.Assinaturas
                .IgnoreQueryFilters()
                .Include(a => a.Plano)
                .FirstOrDefault(a => a.GatewayAssinaturaId == idNoGateway);
        }

        public void Trocar(Assinatura? atual, Assinatura nova)
        {
            using var transacao = _context.Database.BeginTransaction();

            if (atual != null)
            {
                atual.Status = StatusAssinatura.Cancelada;
                atual.CanceladaEm = DateTime.UtcNow;
                _context.SaveChanges(); // libera o índice "uma vigente por empresa" antes de inserir a nova
            }

            _context.Assinaturas.Add(nova);
            _context.SaveChanges();
            transacao.Commit();
        }

        public int ContarPedidosDesde(Guid empresaId, DateTime desdeUtc)
        {
            return _context.Pedidos.IgnoreQueryFilters().Count(p => p.EmpresaId == empresaId && p.CriadoEm >= desdeUtc);
        }

        public List<Guid> ListarUsuariosParaCobranca(Guid empresaId)
        {
            return _context.Usuarios
                .IgnoreQueryFilters()
                .Where(u => u.EmpresaId == empresaId && u.Ativo && u.NotifCobranca)
                .Select(u => u.Id)
                .ToList();
        }

        public void AdicionarNotificacao(Notificacao notificacao)
        {
            _context.Notificacoes.Add(notificacao);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
