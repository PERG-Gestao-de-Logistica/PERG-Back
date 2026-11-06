using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class EntregadorRepository : IEntregadorRepository
    {
        private readonly PergDbContext _context;

        public EntregadorRepository(PergDbContext context)
        {
            _context = context;
        }

        public bool CpfExiste(string cpf) => _context.Entregadores.Any(e => e.Cpf == cpf);
        public bool TelefoneExiste(string telefone) => _context.Entregadores.Any(e => e.Telefone == telefone);
        public bool EmailExiste(string email) => _context.Entregadores.Any(e => e.Email == email);
        public bool PlacaExiste(string placa) => _context.Veiculos.Any(v => v.Placa == placa);

        public void Cadastrar(Entregador entregador, Veiculo veiculo)
        {
            // entregadores.veiculo_atual_id aponta para veiculos e veiculos.entregador_id aponta para entregadores:
            // grava os dois primeiro e liga o veículo atual depois, na mesma transação
            using var transacao = _context.Database.BeginTransaction();

            Guid veiculoId = veiculo.Id;
            entregador.VeiculoAtualId = null;
            _context.Entregadores.Add(entregador);
            _context.Veiculos.Add(veiculo);
            _context.SaveChanges();

            entregador.VeiculoAtualId = veiculoId;
            _context.SaveChanges();

            transacao.Commit();
        }

        public Entregador? ObterPorLogin(string cpfOuTelefone)
        {
            return _context.Entregadores
                .Include(e => e.VeiculoAtual)
                .FirstOrDefault(e => e.Cpf == cpfOuTelefone || e.Telefone == cpfOuTelefone);
        }

        public Entregador? ObterPorId(Guid id)
        {
            return _context.Entregadores
                .Include(e => e.VeiculoAtual)
                .FirstOrDefault(e => e.Id == id);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }

        public int AprovarDocumentosPendentes(Guid entregadorId)
        {
            return _context.DocumentosEntregador
                .Where(d => d.EntregadorId == entregadorId && d.Status == StatusDocumento.Pendente)
                .ExecuteUpdate(s => s
                    .SetProperty(d => d.Status, StatusDocumento.Aprovado)
                    .SetProperty(d => d.AnalisadoEm, (DateTime?)DateTime.UtcNow));
        }

        public List<VwEntregadorRede> ListarRede(TipoVeiculo? tipo)
        {
            IQueryable<VwEntregadorRede> consulta = _context.VwEntregadoresRede.AsNoTracking();

            if (tipo.HasValue)
            {
                string tipoTexto = ConversorEnumSnakeCase<TipoVeiculo>.ParaTexto(tipo.Value);
                consulta = consulta.Where(e => e.VeiculoTipo == tipoTexto);
            }

            return consulta.OrderBy(e => e.Nome).ToList();
        }

        // Favoritos da loja logada (o filtro por empresa do contexto já restringe)
        public List<Guid> ListarFavoritosIds()
        {
            return _context.EmpresaFavoritos.Select(f => f.EntregadorId).ToList();
        }

        public EmpresaFavorito? ObterFavorito(Guid entregadorId)
        {
            return _context.EmpresaFavoritos.FirstOrDefault(f => f.EntregadorId == entregadorId);
        }

        public void AdicionarFavorito(EmpresaFavorito favorito)
        {
            _context.EmpresaFavoritos.Add(favorito);
            _context.SaveChanges();
        }

        public void RemoverFavorito(EmpresaFavorito favorito)
        {
            _context.EmpresaFavoritos.Remove(favorito);
            _context.SaveChanges();
        }
    }
}
