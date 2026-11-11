using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly PergDbContext _context;

        public BackofficeRepository(PergDbContext context)
        {
            _context = context;
        }

        public Administrador? ObterAdministrador(Guid id) => _context.Administradores.FirstOrDefault(a => a.Id == id);

        public Administrador? ObterAdministradorPorEmail(string email) => _context.Administradores.FirstOrDefault(a => a.Email == email);

        public List<Administrador> ListarAdministradores() =>
            _context.Administradores.AsNoTracking().OrderByDescending(a => a.Ativo).ThenBy(a => a.Nome).ToList();

        public bool ExisteAdministrador() => _context.Administradores.Any();

        public bool EmailAdministradorExiste(string email) => _context.Administradores.Any(a => a.Email == email);

        public void AdicionarAdministrador(Administrador administrador)
        {
            _context.Administradores.Add(administrador);
            _context.SaveChanges();
        }

        public List<Entregador> ListarEntregadores(StatusEntregador? status, string? busca, int quantidade)
        {
            IQueryable<Entregador> consulta = _context.Entregadores
                .AsNoTracking()
                .Include(e => e.VeiculoAtual)
                .Include(e => e.DocumentosEntregador);

            if (status.HasValue)
            {
                consulta = consulta.Where(e => e.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                string termo = busca.Trim();
                string digitos = new string(termo.Where(char.IsDigit).ToArray());
                consulta = consulta.Where(e => EF.Functions.ILike(e.Nome, $"%{termo}%")
                    || EF.Functions.ILike(e.Email, $"%{termo}%")
                    || (digitos.Length > 0 && (e.Cpf.Contains(digitos) || e.Telefone.Contains(digitos))));
            }

            // Fila de análise: os mais antigos primeiro
            return consulta.OrderBy(e => e.CriadoEm).Take(quantidade).ToList();
        }

        public Entregador? ObterEntregador(Guid id)
        {
            return _context.Entregadores
                .Include(e => e.VeiculoAtual)
                .Include(e => e.DocumentosEntregador)
                .FirstOrDefault(e => e.Id == id);
        }

        public DocumentoEntregador? ObterDocumento(Guid id)
        {
            return _context.DocumentosEntregador.Include(d => d.Entregador).FirstOrDefault(d => d.Id == id);
        }

        public bool TemRotaEmAndamento(Guid entregadorId)
        {
            return _context.Rotas.IgnoreQueryFilters().Any(r => r.EntregadorId == entregadorId && r.Status == StatusRota.EmAndamento);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
