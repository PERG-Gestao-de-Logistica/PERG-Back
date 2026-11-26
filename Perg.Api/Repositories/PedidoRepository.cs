using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;
using Npgsql;

namespace Perg.Api.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly PergDbContext _context;

        public PedidoRepository(PergDbContext context)
        {
            _context = context;
        }

        // Pronto para a roteirização de hoje: novo, com localização ok, não é retirada, janela não vencida
        // e (mesmo dia ou agendado com janela começando até o fim de hoje)
        private static Expression<Func<Pedido, bool>> Pronto(DateTime agora, DateTime fimDoDia) => p =>
            p.Status == StatusPedido.Novo
            && p.Modalidade != ModalidadePedido.Retirada
            && p.GeocodificacaoStatus == StatusGeocodificacao.Ok
            && p.Latitude != null && p.Longitude != null
            && (p.JanelaFim == null || p.JanelaFim > agora)
            && (p.Modalidade == ModalidadePedido.MesmoDia || p.JanelaInicio == null || p.JanelaInicio < fimDoDia);

        // Com problema: novo de entrega que a roteirização não aceita até alguém corrigir
        private static Expression<Func<Pedido, bool>> ComProblema(DateTime agora) => p =>
            p.Status == StatusPedido.Novo
            && p.Modalidade != ModalidadePedido.Retirada
            && (p.GeocodificacaoStatus != StatusGeocodificacao.Ok
                || p.Latitude == null || p.Longitude == null
                || (p.JanelaFim != null && p.JanelaFim <= agora));

        private static IQueryable<Pedido> FiltrarSituacao(IQueryable<Pedido> consulta, string? situacao, DateTime agora, DateTime fimDoDia)
        {
            return (situacao ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "novos" => consulta.Where(p => p.Status == StatusPedido.Novo),
                "prontos" => consulta.Where(Pronto(agora, fimDoDia)),
                "problemas" => consulta.Where(ComProblema(agora)),
                "emrota" => consulta.Where(p => p.Status == StatusPedido.Roteirizado || p.Status == StatusPedido.EmRota),
                "entregues" => consulta.Where(p => p.Status == StatusPedido.Entregue),
                "ocorrencias" => consulta.Where(p => p.Status == StatusPedido.Ocorrencia),
                "cancelados" => consulta.Where(p => p.Status == StatusPedido.Cancelado),
                _ => consulta
            };
        }

        public Dictionary<string, int> Contar(DateTime agoraUtc, DateTime fimDoDiaUtc)
        {
            IQueryable<Pedido> todos = _context.Pedidos.AsNoTracking();

            // Uma consulta agrupada por status + duas para as abas calculadas
            Dictionary<StatusPedido, int> porStatus = todos
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Quantidade = g.Count() })
                .ToDictionary(x => x.Status, x => x.Quantidade);

            int De(StatusPedido status) => porStatus.TryGetValue(status, out int n) ? n : 0;

            return new Dictionary<string, int>
            {
                ["todos"] = porStatus.Values.Sum(),
                ["novos"] = De(StatusPedido.Novo),
                ["prontos"] = todos.Count(Pronto(agoraUtc, fimDoDiaUtc)),
                ["problemas"] = todos.Count(ComProblema(agoraUtc)),
                ["emRota"] = De(StatusPedido.Roteirizado) + De(StatusPedido.EmRota),
                ["entregues"] = De(StatusPedido.Entregue),
                ["ocorrencias"] = De(StatusPedido.Ocorrencia),
                ["cancelados"] = De(StatusPedido.Cancelado)
            };
        }

        public (List<Pedido> Itens, int Total) Listar(StatusPedido? status, DateTime? criadoDesde, DateTime? criadoAte,
            string? busca, int pagina, int tamanhoPagina)
            => Listar(status, null, DateTime.UtcNow, DateTime.UtcNow, criadoDesde, criadoAte, busca, pagina, tamanhoPagina);

        public (List<Pedido> Itens, int Total) Listar(StatusPedido? status, string? situacao, DateTime agoraUtc, DateTime fimDoDiaUtc,
            DateTime? criadoDesde, DateTime? criadoAte, string? busca, int pagina, int tamanhoPagina)
        {
            IQueryable<Pedido> consulta = _context.Pedidos.AsNoTracking();

            if (status.HasValue)
            {
                consulta = consulta.Where(p => p.Status == status.Value);
            }

            consulta = FiltrarSituacao(consulta, situacao, agoraUtc, fimDoDiaUtc);

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

        public bool ExisteIdExterno(CanalPedido canal, string idExterno)
        {
            return _context.Pedidos.Any(p => p.Canal == canal && p.IdExterno == idExterno);
        }

        public Pedido? ObterPorIdExterno(CanalPedido canal, string idExterno)
        {
            return _context.Pedidos.FirstOrDefault(p => p.Canal == canal && p.IdExterno == idExterno);
        }

        public List<Pedido> ListarAtualizadosDesde(DateTime desdeUtc, int quantidade)
        {
            return _context.Pedidos
                .AsNoTracking()
                .Where(p => p.AtualizadoEm > desdeUtc)
                .OrderBy(p => p.AtualizadoEm)
                .Take(quantidade)
                .ToList();
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
                .Include(p => p.Paradas).ThenInclude(pa => pa.Rota).ThenInclude(r => r.Veiculo)
                .FirstOrDefault(p => p.TokenRastreamento == token);
        }

        public bool TentarAdicionar(Pedido pedido, PedidoEvento evento, Outbox mensagem)
        {
            try
            {
                Adicionar(pedido, evento, mensagem);
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_pedidos_externo" })
            {
                // Descarta o pedido que não entrou, para a próxima consulta ler o que está no banco
                _context.ChangeTracker.Clear();
                return false;
            }
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
