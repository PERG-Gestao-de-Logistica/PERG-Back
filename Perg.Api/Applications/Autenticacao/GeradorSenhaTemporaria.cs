using System.Security.Cryptography;

namespace Perg.Api.Applications.Autenticacao
{
    // Senha provisória para novos membros e redefinição pelo admin.
    // Sem letras/números que se confundem (0/O, 1/l/I), para ditar ou digitar sem erro.
    public static class GeradorSenhaTemporaria
    {
        private const string Caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static string Gerar(int tamanho = 10)
        {
            return RandomNumberGenerator.GetString(Caracteres, tamanho);
        }
    }
}
