using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Autenticacao
{
    // O papel é conferido no banco, não no token: quem perdeu o admin perde a permissão na hora.
    public static class PermissaoAdmin
    {
        public static Usuario Exigir(IUsuarioRepository usuarios, Guid usuarioId, string mensagem)
        {
            Usuario? usuario = usuarios.ObterPorId(usuarioId);
            if (usuario == null || !usuario.Ativo || usuario.Papel != PapelUsuario.Admin)
            {
                throw new AcessoNegadoException(mensagem);
            }
            return usuario;
        }
    }
}
