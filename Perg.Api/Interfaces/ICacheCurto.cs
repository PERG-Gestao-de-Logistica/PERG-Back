namespace Perg.Api.Interfaces
{
    // Cache simples de texto com validade (memória ou Redis). Usado na checagem de acesso a cada requisição.
    public interface ICacheCurto
    {
        string? Obter(string chave);
        void Gravar(string chave, string valor, TimeSpan validade);
        void Remover(string chave);
    }
}
