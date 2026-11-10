namespace Perg.Api.DTOs.UsuariosDto
{
    public class TrocarSenhaDto
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    public class AtualizarPerfilDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool NotifOcorrencias { get; set; }
        public bool NotifCobranca { get; set; }
    }
}
