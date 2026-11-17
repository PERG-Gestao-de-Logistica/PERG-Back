namespace Perg.Api.Interfaces
{
    public interface ICacheCurto
    {
        string? Obter(string chave);
        void Gravar(string chave, string valor, TimeSpan validade);
        void Remover(string chave);
    }
}
