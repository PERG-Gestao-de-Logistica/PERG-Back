using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IUsuarioRepository
    {
        Usuario? ObterPorEmail(string email);
        Usuario? ObterPorId(Guid id);
        bool EmailExiste(string email);
        void Atualizar(Usuario usuario);
    }
}
