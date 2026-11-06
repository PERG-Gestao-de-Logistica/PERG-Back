using System.Text.RegularExpressions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Arquivos
{
    // Guarda os arquivos numa pasta do servidor (Armazenamento:PastaLocal, padrão "armazenamento").
    // A pasta fica fora do wwwroot: nada é servido direto, tudo passa pelos endpoints com permissão.
    public class ArmazenamentoLocal : IArmazenamentoArquivos
    {
        // Só letras minúsculas, números, hífen, sublinhado e barra: impede "../" e caminhos estranhos
        private static readonly Regex ChaveValida = new("^[a-z0-9][a-z0-9_\\-/]*\\.(jpg|png|webp|pdf)$", RegexOptions.Compiled);

        private readonly string _raiz;

        public ArmazenamentoLocal(IConfiguration config, IWebHostEnvironment ambiente)
        {
            string pasta = config["Armazenamento:PastaLocal"] ?? "armazenamento";
            _raiz = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, pasta));
            Directory.CreateDirectory(_raiz);
        }

        public string Salvar(Stream conteudo, string pasta, string extensao)
        {
            string chave = $"{pasta.Trim('/').ToLowerInvariant()}/{Guid.CreateVersion7():N}.{extensao}";
            string caminho = CaminhoSeguro(chave);

            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            using FileStream arquivo = File.Create(caminho);
            conteudo.CopyTo(arquivo);

            return chave;
        }

        public Stream? Abrir(string chave)
        {
            string caminho = CaminhoSeguro(chave);
            return File.Exists(caminho) ? File.OpenRead(caminho) : null;
        }

        public void Remover(string chave)
        {
            string caminho = CaminhoSeguro(chave);
            if (File.Exists(caminho))
            {
                File.Delete(caminho);
            }
        }

        private string CaminhoSeguro(string chave)
        {
            if (!ChaveValida.IsMatch(chave) || chave.Contains(".."))
            {
                throw new ArgumentException("Chave de arquivo inválida.");
            }

            string caminho = Path.GetFullPath(Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar)));
            if (!caminho.StartsWith(_raiz + Path.DirectorySeparatorChar))
            {
                throw new ArgumentException("Chave de arquivo inválida.");
            }

            return caminho;
        }
    }
}
