using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Perg.Api.Contexts.Conversores
{
    // Converte enum <-> texto snake_case, igual aos CHECKs do banco.
    // Ex.: PapelUsuario.Funcionario <-> "funcionario", StatusPedido.EmRota <-> "em_rota"
    public class ConversorEnumSnakeCase<TEnum> : ValueConverter<TEnum, string>
        where TEnum : struct, Enum
    {
        public ConversorEnumSnakeCase()
            : base(valor => ParaTexto(valor), texto => ParaEnum(texto))
        {
        }

        public static string ParaTexto(TEnum valor)
        {
            return Regex.Replace(valor.ToString(), "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
        }

        public static TEnum ParaEnum(string texto)
        {
            return Enum.Parse<TEnum>(texto.Replace("_", ""), ignoreCase: true);
        }
    }
}
