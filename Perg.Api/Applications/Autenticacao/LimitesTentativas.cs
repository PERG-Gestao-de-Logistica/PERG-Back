namespace Perg.Api.Applications.Autenticacao
{
    // Limite de tentativas contra força bruta (configurado no Program.cs). O PIN de entrega tem limite próprio por parada.
    public static class LimitesTentativas
    {
        // Login, esqueci/redefinir senha e códigos: 10 por minuto por IP
        public const string Acesso = "acesso";
    }
}
