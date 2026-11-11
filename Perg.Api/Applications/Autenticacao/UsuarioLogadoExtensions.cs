using System.Security.Claims;

namespace Perg.Api.Applications.Autenticacao
{
    // Atalhos para ler os dados do token dentro dos controllers: User.ObterEmpresaId()
    public static class UsuarioLogadoExtensions
    {
        public static Guid ObterUsuarioId(this ClaimsPrincipal usuario)
        {
            return Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        public static Guid ObterEmpresaId(this ClaimsPrincipal usuario)
        {
            return Guid.Parse(usuario.FindFirstValue(GeradorTokenJwt.ClaimEmpresaId)!);
        }

        public static Guid ObterEntregadorId(this ClaimsPrincipal usuario)
        {
            return Guid.Parse(usuario.FindFirstValue(GeradorTokenJwt.ClaimEntregadorId)!);
        }

        public static Guid ObterAdministradorId(this ClaimsPrincipal usuario)
        {
            return Guid.Parse(usuario.FindFirstValue(GeradorTokenJwt.ClaimAdministradorId)!);
        }

        public static string ObterNome(this ClaimsPrincipal usuario)
        {
            return usuario.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }
    }
}
