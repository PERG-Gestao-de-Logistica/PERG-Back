namespace Perg.Api.DTOs.UsuariosDto
{
    // "credential" que o botão do Google devolve ao front
    public class LoginGoogleDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
