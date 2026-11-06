using System.Text.RegularExpressions;

namespace Perg.Api.Applications.Pagamentos
{
    // Tipos aceitos pelo Asaas em pixAddressKeyType
    public enum TipoChavePix { Cpf, Cnpj, Email, Phone, Evp }

    public record ChavePixNormalizada(string Valor, TipoChavePix Tipo);

    // Descobre o tipo da chave e deixa no formato que o banco espera
    public static class ChavePix
    {
        private static readonly Regex Evp = new("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);
        private static readonly Regex Email = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");

        public static ChavePixNormalizada? Normalizar(string? chave)
        {
            if (string.IsNullOrWhiteSpace(chave)) return null;
            string texto = chave.Trim();

            if (Evp.IsMatch(texto)) return new(texto.ToLowerInvariant(), TipoChavePix.Evp);
            if (Email.IsMatch(texto)) return new(texto.ToLowerInvariant(), TipoChavePix.Email);

            string digitos = new(texto.Where(char.IsDigit).ToArray());

            // Celular com +55 na frente
            if (texto.StartsWith("+55") && digitos.Length == 13) return new(digitos[2..], TipoChavePix.Phone);
            if (digitos.Length == 14) return new(digitos, TipoChavePix.Cnpj);

            // 11 dígitos pode ser CPF ou celular: CPF válido ganha
            if (digitos.Length == 11) return CpfValido(digitos) ? new(digitos, TipoChavePix.Cpf) : new(digitos, TipoChavePix.Phone);

            return null;
        }

        private static bool CpfValido(string cpf)
        {
            if (cpf.Distinct().Count() == 1) return false;

            int Digito(int tamanho)
            {
                int soma = 0;
                for (int i = 0; i < tamanho; i++) soma += (cpf[i] - '0') * (tamanho + 1 - i);
                int resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            return Digito(9) == cpf[9] - '0' && Digito(10) == cpf[10] - '0';
        }
    }
}
