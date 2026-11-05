namespace Perg.Api.DTOs.EntregadoresDto
{
    public class LoginEntregadorDto
    {
        // CPF ou celular, só números ou com máscara
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
