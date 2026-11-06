using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class ArquivoRepository : IArquivoRepository
    {
        private readonly PergDbContext _context;

        public ArquivoRepository(PergDbContext context)
        {
            _context = context;
        }

        public Empresa? ObterEmpresa(Guid empresaId) => _context.Empresas.FirstOrDefault(e => e.Id == empresaId);

        public Usuario? ObterUsuario(Guid usuarioId) => _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);

        public Entregador? ObterEntregador(Guid entregadorId)
        {
            return _context.Entregadores.Include(e => e.VeiculoAtual).FirstOrDefault(e => e.Id == entregadorId);
        }

        public Veiculo? ObterVeiculo(Guid veiculoId, Guid entregadorId)
        {
            return _context.Veiculos.FirstOrDefault(v => v.Id == veiculoId && v.EntregadorId == entregadorId);
        }

        public Parada? ObterParadaComComprovante(Guid pedidoId)
        {
            return _context.Paradas
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId && p.Status == StatusParada.Entregue && p.ComprovanteChave != null)
                .OrderByDescending(p => p.ConcluidaEm)
                .FirstOrDefault();
        }

        public Ocorrencia? ObterOcorrencia(Guid ocorrenciaId)
        {
            return _context.Ocorrencias.AsNoTracking().FirstOrDefault(o => o.Id == ocorrenciaId);
        }

        public List<DocumentoEntregador> ListarDocumentos(Guid entregadorId)
        {
            return _context.DocumentosEntregador
                .AsNoTracking()
                .Where(d => d.EntregadorId == entregadorId)
                .OrderByDescending(d => d.CriadoEm)
                .ToList();
        }

        public bool ExisteDocumentoAtivo(Guid entregadorId, TipoDocumento tipo, Guid? veiculoId)
        {
            return _context.DocumentosEntregador.Any(d => d.EntregadorId == entregadorId
                && d.Tipo == tipo
                && d.VeiculoId == veiculoId
                && d.Status != StatusDocumento.Rejeitado);
        }

        public bool CnhEmUsoPorOutro(string numero, Guid entregadorId)
        {
            return _context.DocumentosEntregador.Any(d => d.Tipo == TipoDocumento.Cnh
                && d.Numero == numero
                && d.EntregadorId != entregadorId
                && d.Status != StatusDocumento.Rejeitado);
        }

        public void Adicionar(object entidade) => _context.Add(entidade);

        public void Salvar() => _context.SaveChanges();
    }
}
