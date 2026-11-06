using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.ArquivosDto
{
    // multipart/form-data com o campo "arquivo"
    public class EnviarArquivoDto
    {
        public IFormFile? Arquivo { get; set; }
    }

    // Para que serve o arquivo enviado pelo app antes de confirmar a entrega/ocorrência
    public enum FinalidadeArquivoApp
    {
        Comprovante = 1,
        Ocorrencia = 2
    }

    public class LerArquivoEnviadoDto
    {
        // Vai no campo comprovanteChave (entregar) ou fotoChave (ocorrência)
        public string Chave { get; set; } = string.Empty;

        // Só para fotos públicas (perfil, loja)
        public string? Url { get; set; }
    }

    // multipart/form-data: arquivo + campos
    public class EnviarDocumentoDto
    {
        public IFormFile? Arquivo { get; set; }
        public TipoDocumento Tipo { get; set; }

        // CNH: número (11 dígitos) e categoria (A, B, AB...)
        public string? Numero { get; set; }
        public string? Categoria { get; set; }

        // CNH com "Exerce Atividade Remunerada"
        public bool? Ear { get; set; }
        public DateOnly? Validade { get; set; }

        // CRLV: veículo do documento (se não vier, usa o veículo atual)
        public Guid? VeiculoId { get; set; }
    }

    public class LerDocumentoDto
    {
        public Guid Id { get; set; }
        public TipoDocumento Tipo { get; set; }
        public StatusDocumento Status { get; set; }
        public string? Categoria { get; set; }
        public DateOnly? Validade { get; set; }
        public string? MotivoRejeicao { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AnalisadoEm { get; set; }
    }
}
