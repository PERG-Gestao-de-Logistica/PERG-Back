namespace Perg.Api.Interfaces
{
    // Hoje pasta local; em produção, Blob Storage ou S3, trocando só a implementação.
    public interface IArmazenamentoArquivos
    {
        // Devolve a chave (caminho) do arquivo, ex.: "comprovantes/{entregador}/{id}.jpg"
        string Salvar(Stream conteudo, string pasta, string extensao);

        Stream? Abrir(string chave);

        void Remover(string chave);
    }
}
