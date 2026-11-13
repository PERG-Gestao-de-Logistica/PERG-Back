using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.UsuariosDto
{
    public class LerUsuarioDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public PapelUsuario Papel { get; set; }
        public bool TrocarSenha { get; set; }
        public bool NotifOcorrencias { get; set; }
        public bool NotifCobranca { get; set; }
        public bool GoogleVinculado { get; set; }
        public Guid EmpresaId { get; set; }
        public string NomeEmpresa { get; set; } = string.Empty;
    }
}
