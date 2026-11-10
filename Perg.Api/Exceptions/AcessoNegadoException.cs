namespace Perg.Api.Exceptions
{
    // Regra de permissão (ex.: só o admin da empresa gerencia a equipe). Os controllers devolvem 403.
    public class AcessoNegadoException : DomainException
    {
        public AcessoNegadoException(string mensagem) : base(mensagem)
        {
        }
    }
}
