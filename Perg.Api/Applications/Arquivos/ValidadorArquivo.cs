using Perg.Api.Exceptions;

namespace Perg.Api.Applications.Arquivos
{
    public record ArquivoValidado(string Extensao, string TipoConteudo);

    // Confere o tamanho e o tipo real do arquivo pelos primeiros bytes (não confia na extensão nem no navegador)
    public static class ValidadorArquivo
    {
        private const long MaximoImagem = 5 * 1024 * 1024;   // 5 MB
        private const long MaximoPdf = 10 * 1024 * 1024;     // 10 MB

        public static ArquivoValidado Validar(IFormFile? arquivo, bool permitirPdf = false)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                throw new DomainException("Envie um arquivo.");
            }

            byte[] cabecalho = new byte[12];
            using (Stream leitura = arquivo.OpenReadStream())
            {
                int lidos = leitura.Read(cabecalho, 0, cabecalho.Length);
                if (lidos < 4)
                {
                    throw new DomainException("Arquivo inválido.");
                }
            }

            ArquivoValidado? tipo = Identificar(cabecalho);

            if (tipo == null || (tipo.Extensao == "pdf" && !permitirPdf))
            {
                throw new DomainException(permitirPdf
                    ? "Formato não aceito. Envie JPG, PNG, WEBP ou PDF."
                    : "Formato não aceito. Envie uma imagem JPG, PNG ou WEBP.");
            }

            long maximo = tipo.Extensao == "pdf" ? MaximoPdf : MaximoImagem;
            if (arquivo.Length > maximo)
            {
                throw new DomainException($"Arquivo muito grande. O limite é {maximo / (1024 * 1024)} MB.");
            }

            return tipo;
        }

        private static ArquivoValidado? Identificar(byte[] b)
        {
            if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
                return new ArquivoValidado("jpg", "image/jpeg");

            if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
                return new ArquivoValidado("png", "image/png");

            if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
                return new ArquivoValidado("webp", "image/webp");

            if (b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F')
                return new ArquivoValidado("pdf", "application/pdf");

            return null;
        }

        public static string TipoConteudoPelaChave(string chave)
        {
            return Path.GetExtension(chave).ToLowerInvariant() switch
            {
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}
