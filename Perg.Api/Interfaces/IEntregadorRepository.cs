using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IEntregadorRepository
    {
        bool CpfExiste(string cpf);
        bool TelefoneExiste(string telefone);
        bool EmailExiste(string email);
        bool PlacaExiste(string placa);

        void Cadastrar(Entregador entregador, Veiculo veiculo);

        Entregador? ObterPorLogin(string cpfOuTelefone);
        Entregador? ObterPorId(Guid id);
        void Salvar();

        // Só para o ambiente de desenvolvimento (aprovar-teste)
        int AprovarDocumentosPendentes(Guid entregadorId);

        // Visão da loja
        List<VwEntregadorRede> ListarRede(TipoVeiculo? tipo);
        List<Guid> ListarFavoritosIds();
        EmpresaFavorito? ObterFavorito(Guid entregadorId);
        void AdicionarFavorito(EmpresaFavorito favorito);
        void RemoverFavorito(EmpresaFavorito favorito);
    }
}
