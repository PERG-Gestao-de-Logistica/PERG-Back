using System.Security.Cryptography;
using System.Text;

namespace Perg.Api.Applications.Integracoes
{
    // Formato das chaves: "perg_" + 40 caracteres aleatórios. Só o hash SHA-256 vai para o banco.
    public static class ChavesApi
    {
        public const string Prefixo = "perg_";
        public const string Cabecalho = "X-Api-Key";
        private const string Caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string Gerar() => Prefixo + RandomNumberGenerator.GetString(Caracteres, 40);

        public static string Hash(string chave) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave.Trim()))).ToLowerInvariant();

        // "perg_Ab12Cd" — o suficiente para a loja reconhecer qual chave é, sem expor o resto
        public static string Inicio(string chave) => chave[..Math.Min(chave.Length, 11)];
    }
}
