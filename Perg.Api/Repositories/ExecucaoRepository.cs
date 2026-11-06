using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    // O token do entregador não tem empresa, então o filtro por empresa fica desligado.
    // Toda consulta aqui filtra pelo entregador logado.
    public class ExecucaoRepository : IExecucaoRepository
    {
        private readonly PergDbContext _context;

        public ExecucaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public Entregador? ObterEntregador(Guid entregadorId)
        {
            return _context.Entregadores.Include(e => e.VeiculoAtual).FirstOrDefault(e => e.Id == entregadorId);
        }

        public Jornada? ObterJornadaAberta(Guid entregadorId)
        {
            return _context.Jornadas
                .Include(j => j.Veiculo)
                .FirstOrDefault(j => j.EntregadorId == entregadorId && j.EncerradaEm == null);
        }

        public void AdicionarJornada(Jornada jornada)
        {
            _context.Jornadas.Add(jornada);
            _context.SaveChanges();
        }

        public bool TemRotaEmAndamento(Guid entregadorId)
        {
            return _context.Rotas.IgnoreQueryFilters()
                .Any(r => r.EntregadorId == entregadorId && r.Status == StatusRota.EmAndamento);
        }

        public List<Rota> ListarRotasDoEntregador(Guid entregadorId)
        {
            return ConsultaRotas()
                .AsNoTracking()
                .Where(r => r.EntregadorId == entregadorId
                    && (r.Status == StatusRota.Aceita || r.Status == StatusRota.EmAndamento))
                .OrderBy(r => r.ColetaPrevistaEm)
                .ToList();
        }

        public Rota? ObterRotaDoEntregador(Guid rotaId, Guid entregadorId)
        {
            return ConsultaRotas().FirstOrDefault(r => r.Id == rotaId && r.EntregadorId == entregadorId);
        }

        public Parada? ObterParadaDoEntregador(Guid paradaId, Guid entregadorId)
        {
            return _context.Paradas
                .IgnoreQueryFilters()
                .Include(p => p.Pedido)
                .Include(p => p.Rota).ThenInclude(r => r.Paradas).ThenInclude(x => x.Pedido)
                .Include(p => p.Rota).ThenInclude(r => r.Empresa)
                .FirstOrDefault(p => p.Id == paradaId && p.Rota.EntregadorId == entregadorId);
        }

        public List<Guid> ListarUsuariosParaNotificar(Guid empresaId, bool ocorrencia)
        {
            return _context.Usuarios
                .IgnoreQueryFilters()
                .Where(u => u.EmpresaId == empresaId && u.Ativo
                    && (ocorrencia ? u.NotifOcorrencias : u.Papel == PapelUsuario.Admin))
                .Select(u => u.Id)
                .ToList();
        }

        public decimal ObterSaldo(Guid entregadorId)
        {
            return _context.CarteiraLancamentos
                .Where(l => l.EntregadorId == entregadorId)
                .Sum(l => (decimal?)l.Valor) ?? 0m;
        }

        public List<CarteiraLancamento> ListarLancamentos(Guid entregadorId, int quantidade)
        {
            return _context.CarteiraLancamentos
                .AsNoTracking()
                .Where(l => l.EntregadorId == entregadorId)
                .OrderByDescending(l => l.CriadoEm)
                .Take(quantidade)
                .ToList();
        }

        public void Adicionar(object entidade)
        {
            _context.Add(entidade);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }

        private IQueryable<Rota> ConsultaRotas()
        {
            return _context.Rotas
                .IgnoreQueryFilters()
                .Include(r => r.Empresa)
                .Include(r => r.Roteirizacao).ThenInclude(rz => rz.Origem)
                .Include(r => r.Paradas.OrderBy(p => p.Sequencia)).ThenInclude(p => p.Pedido);
        }
    }
}
