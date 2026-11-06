using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly PergDbContext _context;

        public PedidoRepository(PergDbContext context)
        {
            _context = context;
        }

        public (List<Pedido> Itens, int Total) Listar(StatusPedido? status, DateTime? criadoDesde, DateTime? criadoAte,
            string? busca, int pagina, int tamanhoPagina)
        {
            IQueryable<Pedido> consulta = _context.Pedidos.AsNoTracking();

            if (status.HasValue)
            {
                consulta = consulta.Where(p => p.Status == status.Value);
            }

            if (criadoDesde.HasValue)
            {
                consulta = consulta.Where(p => p.CriadoEm >= criadoDesde.Value);
            }

            if (criadoAte.HasValue)
            {
                consulta = consulta.Where(p => p.CriadoEm < criadoAte.Value);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                string termo = busca.Trim();
                string padrao = "%" + termo + "%";
                bool ehNumero = int.TryParse(termo, out int numero);

                consulta = consulta.Where(p =>
                    (ehNumero && p.NumeroPedido == numero)
                    || EF.Functions.ILike(p.ClienteNome, padrao)
                    || (p.NumeroExterno != null && EF.Functions.ILike(p.NumeroExterno, padrao)));
            }

            int total = consulta.Count();

            List<Pedido> itens = consulta
                .OrderByDescending(p => p.CriadoEm)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToList();

            return (itens, total);
        }

        public Pedido? ObterPorId(Guid id)
        {
            return _context.Pedidos
                .Include(p => p.Origem)
                .Include(p => p.PedidoEventos.OrderBy(e => e.OcorridoEm))
                .FirstOrDefault(p => p.Id == id);
        }

        // Rota pública: o token é a "senha" do link, então ignora o filtro por empresa
        public Pedido? ObterPorToken(string token)
        {
            return _context.Pedidos
                .IgnoreQueryFilters()
                // Pedido -> parada -> rota -> paradas volta para o mesmo tipo de entidade (ciclo):
                // sem rastreamento, isso só é permitido com resolução de identidade
                .AsNoTrackingWithIdentityResolution()
                .Include(p => p.Empresa)
                .Include(p => p.PedidoEventos.OrderBy(e => e.OcorridoEm))
                .Include(p => p.Paradas).ThenInclude(pa => pa.Rota).ThenInclude(r => r.Paradas)
                .FirstOrDefault(p => p.TokenRastreamento == token);
        }

        public void Adicionar(Pedido pedido, PedidoEvento evento, Outbox mensagem)
        {
            _context.Pedidos.Add(pedido);
            _context.PedidoEventos.Add(evento);
            _context.Outbox.Add(mensagem);
            _context.SaveChanges();
        }

        public void Atualizar(Pedido pedido, PedidoEvento evento, Outbox mensagem)
        {
            // O pedido veio deste contexto (rastreado); basta registrar os eventos e salvar
            _context.PedidoEventos.Add(evento);
            _context.Outbox.Add(mensagem);
            _context.SaveChanges();
        }

        public void Atualizar(Pedido pedido)
        {
            _context.SaveChanges();
        }
    }
}
