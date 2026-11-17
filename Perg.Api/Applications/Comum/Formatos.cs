using Perg.Api.Domains;

namespace Perg.Api.Applications.Comum
{
    public static class Formatos
    {
        public static string SomenteDigitos(string? texto) => new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());

        public static string CodigoRota(short numero) => "R" + numero.ToString("00");

        public static string CodigoRota(Rota rota) => CodigoRota(rota.Numero);
    }
}
