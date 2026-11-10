namespace Perg.Api.DTOs.RecuperacaoSenhaDto
{
    // Loja: identifica pelo e-mail
    public class EsqueciSenhaUsuarioDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RedefinirSenhaUsuarioDto
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    // App: identifica pelo CPF ou celular (o mesmo do login); o código vai para o e-mail cadastrado
    public class EsqueciSenhaEntregadorDto
    {
        public string Login { get; set; } = string.Empty;
    }

    public class RedefinirSenhaEntregadorDto
    {
        public string Login { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }
}
