using System.Security.Claims;

namespace Perg.Api.Applications.Autenticacao
{
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
    }
}
