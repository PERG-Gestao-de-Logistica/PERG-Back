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

        public VwEntregadorRede? ObterNaRede(Guid id) =>
            _context.VwEntregadoresRede.AsNoTracking().FirstOrDefault(e => e.Id == id);

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

        // Entregas e falhas de cada entregador só nesta loja (o filtro global por empresa já se aplica).
        // Mesma regra da vw_entregadores_rede: falha com ocorrência de culpa do cliente não conta.
        public Dictionary<Guid, (long Entregues, long Falhas)> ContarEntregasDaLoja()
        {
            var paradas = _context.Paradas.AsNoTracking()
                .Where(p => p.Rota.EntregadorId != null
                    && (p.Status == StatusParada.Entregue || p.Status == StatusParada.Falhou))
                .Select(p => new { p.Id, EntregadorId = p.Rota.EntregadorId!.Value, p.Status })
                .ToList();

            var ocorrencias = _context.Ocorrencias.AsNoTracking()
                .Select(o => new { o.ParadaId, o.Tipo })
                .ToList();
            HashSet<Guid> culpaDoCliente = ocorrencias
                .Where(o => o.Tipo == TipoOcorrencia.ClienteAusente
                    || o.Tipo == TipoOcorrencia.EnderecoNaoEncontrado
                    || o.Tipo == TipoOcorrencia.Recusado)
                .Select(o => o.ParadaId)
                .ToHashSet();

            return paradas
                .GroupBy(p => p.EntregadorId)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        (long)g.Count(p => p.Status == StatusParada.Entregue),
                        (long)g.Count(p => p.Status == StatusParada.Falhou && !culpaDoCliente.Contains(p.Id))));
        }

        public HashSet<Guid> ListarComJornadaAberta()
        {
            return _context.Jornadas.AsNoTracking()
                .Where(j => j.EncerradaEm == null)
                .Select(j => j.EntregadorId)
                .ToHashSet();
        }

        // Entregador -> empresa da rota que ele aceitou ou está fazendo agora (de qualquer loja)
        public Dictionary<Guid, Guid> ListarEmpresaDaRotaAtual()
        {
            return _context.Rotas.IgnoreQueryFilters().AsNoTracking()
                .Where(r => r.EntregadorId != null
                    && (r.Status == StatusRota.Aceita || r.Status == StatusRota.EmAndamento))
                .Select(r => new { EntregadorId = r.EntregadorId!.Value, r.EmpresaId, r.Status })
                .ToList()
                .GroupBy(r => r.EntregadorId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.Status == StatusRota.EmAndamento).First().EmpresaId);
        }

        public Origem? ObterOrigemPadrao()
        {
            return _context.Origens.AsNoTracking().FirstOrDefault(o => o.Padrao && o.Ativa);
        }
    }
}
