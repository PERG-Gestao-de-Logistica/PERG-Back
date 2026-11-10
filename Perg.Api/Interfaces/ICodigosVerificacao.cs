namespace Perg.Api.Interfaces
{
    public enum ResultadoCodigo { Valido, Invalido, Expirado, TentativasEsgotadas }

    // Códigos de 6 dígitos (esqueci a senha; depois, confirmação de e-mail/SMS).
    // Hoje em memória; com Redis, troca só esta implementação.
    public interface ICodigosVerificacao
    {
        // null = um código foi gerado para esta chave há menos de 1 minuto (evita spam de e-mail)
        string? Gerar(string chave);

        // Válido consome o código: não serve de novo
        ResultadoCodigo Validar(string chave, string codigo);
    }
}
