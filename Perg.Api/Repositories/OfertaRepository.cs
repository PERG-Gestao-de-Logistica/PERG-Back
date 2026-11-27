using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class OfertaRepository : IOfertaRepository
    {
        private readonly PergDbContext _context;

        public OfertaRepository(PergDbContext context)
        {
            _context = context;
        }

        public Rota? ObterRota(Guid rotaId)
        {
            return _context.Rotas
                .Include(r => r.OfertasRota)
                .Include(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                .Include(r => r.Entregador)
                .Include(r => r.Paradas.OrderBy(p => p.Sequencia))
                    .ThenInclude(p => p.Pedido)
                .FirstOrDefault(r => r.Id == rotaId);
        }

        public Empresa? ObterEmpresa(Guid empresaId)
        {
            return _context.Empresas.FirstOrDefault(e => e.Id == empresaId);
        }

        public List<Guid> ListarLivresEmExpediente(TipoVeiculo tipo, Guid? favoritosDaEmpresa, List<Guid> excluir)
        {
            IQueryable<Entregador> consulta = _context.Entregadores
                .Where(e => e.Status == StatusEntregador.Ativo
                    && e.VeiculoAtual != null
                    && e.VeiculoAtual.Ativo
                    && e.VeiculoAtual.Tipo == tipo
                    && !excluir.Contains(e.Id)
                    && _context.Jornadas.Any(j => j.EntregadorId == e.Id && j.EncerradaEm == null)
                    && !_context.Rotas.IgnoreQueryFilters().Any(r => r.EntregadorId == e.Id
                        && (r.Status == StatusRota.Aceita || r.Status == StatusRota.EmAndamento)));

            if (favoritosDaEmpresa is Guid empresaId)
            {
                consulta = consulta.Where(e => _context.EmpresaFavoritos.IgnoreQueryFilters()
                    .Any(f => f.EmpresaId == empresaId && f.EntregadorId == e.Id));
            }

            return consulta.Select(e => e.Id).ToList();
        }

        public bool EstaComRota(Guid entregadorId)
        {
            return _context.Rotas.IgnoreQueryFilters().Any(r => r.EntregadorId == entregadorId
                && (r.Status == StatusRota.Aceita || r.Status == StatusRota.EmAndamento));
        }

        public bool TemJornadaAberta(Guid entregadorId)
        {
            return _context.Jornadas.Any(j => j.EntregadorId == entregadorId && j.EncerradaEm == null);
        }

        public List<OfertaRota> ListarAbertasDoEntregador(Guid entregadorId, DateTime agora)
        {
            return _context.OfertasRota
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(o => o.Empresa)
                .Include(o => o.Rota).ThenInclude(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                .Include(o => o.Rota).ThenInclude(r => r.Paradas).ThenInclude(p => p.Pedido)
                .Where(o => o.EntregadorId == entregadorId
                    && o.Status == StatusOferta.Enviada
                    && o.ExpiraEm > agora
                    && o.Rota.Status == StatusRota.Ofertada)
                .OrderBy(o => o.ExpiraEm)
                .ToList();
        }

        public OfertaRota? ObterParaResposta(Guid ofertaId, Guid entregadorId)
        {
            return _context.OfertasRota
                .IgnoreQueryFilters()
                .Include(o => o.Entregador).ThenInclude(e => e.VeiculoAtual)
                .Include(o => o.Rota).ThenInclude(r => r.OfertasRota)
                .Include(o => o.Rota).ThenInclude(r => r.Paradas).ThenInclude(p => p.Pedido)
                .FirstOrDefault(o => o.Id == ofertaId && o.EntregadorId == entregadorId);
        }

        public List<OfertaRota> ListarDaRota(Guid rotaId)
        {
            return _context.OfertasRota
                .AsNoTracking()
                .Include(o => o.Entregador)
                .Where(o => o.RotaId == rotaId)
                .OrderBy(o => o.EnviadaEm)
                .ToList();
        }

        public List<OfertaRota> ListarVencidas(DateTime agora)
        {
            return _context.OfertasRota
                .IgnoreQueryFilters()
                .Where(o => o.Status == StatusOferta.Enviada && o.ExpiraEm <= agora)
                .ToList();
        }

        public List<Rota> ListarOfertadasSemOfertaAberta()
        {
            return _context.Rotas
                .IgnoreQueryFilters()
                .Include(r => r.OfertasRota)
                .Include(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                .Where(r => r.Status == StatusRota.Ofertada
                    && !r.OfertasRota.Any(o => o.Status == StatusOferta.Enviada || o.Status == StatusOferta.Aceita))
                .ToList();
        }

        public List<Guid> ListarAdminsDaEmpresa(Guid empresaId)
        {
            return _context.Usuarios
                .IgnoreQueryFilters()
                .Where(u => u.EmpresaId == empresaId && u.Ativo && u.Papel == PapelUsuario.Admin)
                .Select(u => u.Id)
                .ToList();
        }

        public void Adicionar(IEnumerable<OfertaRota> ofertas)
        {
            _context.OfertasRota.AddRange(ofertas);
        }

        public void Adicionar(IEnumerable<PedidoEvento> eventos, IEnumerable<Outbox> mensagens, IEnumerable<Notificacao> notificacoes)
        {
            _context.PedidoEventos.AddRange(eventos);
            _context.Outbox.AddRange(mensagens);
            _context.Notificacoes.AddRange(notificacoes);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
