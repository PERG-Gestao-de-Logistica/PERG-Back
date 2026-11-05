namespace Perg.Api.Domains.Enums
{
    // Gravado como texto em snake_case: 'admin', 'funcionario'.
    // Os valores começam em 1 de propósito: o 0 (valor padrão do C#) não é um papel válido.
    public enum PapelUsuario
    {
        Admin = 1,
        Funcionario = 2
    }
}
