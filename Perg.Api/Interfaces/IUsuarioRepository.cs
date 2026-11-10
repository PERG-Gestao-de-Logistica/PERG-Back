using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IUsuarioRepository
    {
        Usuario? ObterPorEmail(string email);
        Usuario? ObterPorId(Guid id);
        bool EmailExiste(string email);
        void Atualizar(Usuario usuario);

        // Equipe (já filtrado pela empresa do usuário logado)
        List<Usuario> ListarDaEmpresa(bool incluirInativos);
        int ContarAdminsAtivos();
        void Adicionar(Usuario usuario);
    }
}
