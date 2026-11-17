using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IUsuarioRepository
    {
        Usuario? ObterPorEmail(string email);
        Usuario? ObterPorId(Guid id);
        bool EmailExiste(string email);
        void Atualizar(Usuario usuario);

        Usuario? ObterPorLoginExterno(string provedor, string chave);
        bool TemLoginExterno(Guid usuarioId, string provedor);
        void VincularLoginExterno(Guid usuarioId, string provedor, string chave);
        void RemoverLoginExterno(Guid usuarioId, string provedor);

        List<Usuario> ListarDaEmpresa(bool incluirInativos);
        int ContarAdminsAtivos();
        void Adicionar(Usuario usuario);
    }
}
