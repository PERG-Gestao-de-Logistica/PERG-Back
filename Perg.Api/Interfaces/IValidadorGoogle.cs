namespace Perg.Api.Interfaces
{
    // Dados da conta Google tirados do ID token (já conferido: assinatura, validade e Client ID do Perg)
    public record ContaGoogle(string Id, string Email, bool EmailVerificado, string? Nome);

    public interface IValidadorGoogle
    {
        // Token inválido, vencido ou de outro aplicativo → DomainException
        ContaGoogle Validar(string idToken);
    }
}
