using System.Security.Cryptography;
using System.Text;

namespace Perg.Api.Applications.Execucao
{
    // PIN de 4 dígitos que o cliente vê na página de rastreio e informa ao entregador.
    // É derivado do token de rastreamento, então não precisa de coluna no banco.
    public static class PinEntrega
    {
        public static string Gerar(string tokenRastreamento)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("perg-pin:" + tokenRastreamento));
            uint numero = BitConverter.ToUInt32(hash, 0);
            return (numero % 10000).ToString("D4");
        }
    }
}
