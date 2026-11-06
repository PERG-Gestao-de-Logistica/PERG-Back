namespace Perg.Api.Interfaces
{
    // Onde os arquivos ficam guardados. Hoje: pasta local. Em produção: Azure Blob Storage (ou S3),
    // trocando só a implementação, sem mexer nos services.
    public interface IArmazenamentoArquivos
    {
        // Devolve a chave (caminho) do arquivo, ex.: "comprovantes/{entregador}/{id}.jpg"
        string Salvar(Stream conteudo, string pasta, string extensao);

        Stream? Abrir(string chave);

        void Remover(string chave);
    }
}
