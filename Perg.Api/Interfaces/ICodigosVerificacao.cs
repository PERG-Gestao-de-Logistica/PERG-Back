namespace Perg.Api.Interfaces
{
    public enum ResultadoCodigo { Valido, Invalido, Expirado, TentativasEsgotadas }

    public interface ICodigosVerificacao
    {
        // null = um código foi gerado para esta chave há menos de 1 minuto (evita spam de e-mail)
        string? Gerar(string chave);

        // Válido consome o código: não serve de novo
        ResultadoCodigo Validar(string chave, string codigo);
    }
}
