using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IArquivoRepository
    {
        Empresa? ObterEmpresa(Guid empresaId);
        Usuario? ObterUsuario(Guid usuarioId);
        Entregador? ObterEntregador(Guid entregadorId);
        Veiculo? ObterVeiculo(Guid veiculoId, Guid entregadorId);

        // Loja logada (filtro por empresa)
        Parada? ObterParadaComComprovante(Guid pedidoId);
        Ocorrencia? ObterOcorrencia(Guid ocorrenciaId);

        // Documentos do entregador
        List<DocumentoEntregador> ListarDocumentos(Guid entregadorId);
        bool ExisteDocumentoAtivo(Guid entregadorId, TipoDocumento tipo, Guid? veiculoId);
        bool CnhEmUsoPorOutro(string numero, Guid entregadorId);

        void Adicionar(object entidade);
        void Salvar();
    }
}
