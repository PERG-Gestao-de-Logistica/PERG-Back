using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class SaqueRepository : ISaqueRepository
    {
        private readonly PergDbContext _context;

        public SaqueRepository(PergDbContext context)
        {
            _context = context;
        }

        public Entregador? ObterEntregador(Guid entregadorId)
        {
            return _context.Entregadores.FirstOrDefault(e => e.Id == entregadorId);
        }

        public Saque? RegistrarSaque(Guid entregadorId, decimal valor, string chavePix)
        {
            using var transacao = _context.Database.BeginTransaction();

            // Dois saques ao mesmo tempo esperam um pelo outro aqui: o segundo já vê o saldo atualizado
            _context.Database.ExecuteSql($"SELECT 1 FROM entregadores WHERE id = {entregadorId} FOR UPDATE");

            decimal saldo = _context.CarteiraLancamentos
                .Where(l => l.EntregadorId == entregadorId)
                .Sum(l => (decimal?)l.Valor) ?? 0m;

            if (saldo < valor)
            {
                transacao.Rollback();
                return null;
            }

            DateTime agora = DateTime.UtcNow;
            var saque = new Saque
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = entregadorId,
                Valor = valor,
                ChavePix = chavePix,
                Status = StatusSaque.Solicitado,
                SolicitadoEm = agora
            };

            _context.Saques.Add(saque);
            _context.CarteiraLancamentos.Add(new CarteiraLancamento
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = entregadorId,
                Tipo = TipoLancamento.Saque,
                Valor = -valor,
                SaqueId = saque.Id,
                Descricao = "Saque via Pix"
            });

            _context.SaveChanges();
            transacao.Commit();

            return saque;
        }

        public bool TemSaqueEmAndamento(Guid entregadorId)
        {
            return _context.Saques.Any(s => s.EntregadorId == entregadorId
                && (s.Status == StatusSaque.Solicitado || s.Status == StatusSaque.Processando));
        }

        public List<Saque> ListarDoEntregador(Guid entregadorId, int quantidade)
        {
            return _context.Saques
                .AsNoTracking()
                .Where(s => s.EntregadorId == entregadorId)
                .OrderByDescending(s => s.SolicitadoEm)
                .Take(quantidade)
                .ToList();
        }

        public List<Saque> ListarProcessandoAntesDe(DateTime limite)
        {
            return _context.Saques
                .AsNoTracking()
                .Where(s => s.Status == StatusSaque.Processando && s.SolicitadoEm <= limite)
                .OrderBy(s => s.SolicitadoEm)
                .Take(50)
                .ToList();
        }

        public Saque? ObterPorId(Guid saqueId)
        {
            return _context.Saques.Include(s => s.CarteiraLancamentos).FirstOrDefault(s => s.Id == saqueId);
        }

        public Saque? ObterPorIdNoGateway(string idNoGateway)
        {
            return _context.Saques.Include(s => s.CarteiraLancamentos).FirstOrDefault(s => s.GatewayTransferenciaId == idNoGateway);
        }

        public void Estornar(Saque saque, string motivo)
        {
            if (saque.CarteiraLancamentos.Any(l => l.Tipo == TipoLancamento.EstornoSaque))
            {
                return; // já estornado
            }

            _context.CarteiraLancamentos.Add(new CarteiraLancamento
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = saque.EntregadorId,
                Tipo = TipoLancamento.EstornoSaque,
                Valor = saque.Valor,
                SaqueId = saque.Id,
                Descricao = "Estorno: " + motivo
            });
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
