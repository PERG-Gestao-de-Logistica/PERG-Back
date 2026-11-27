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

        List<VwEntregadorRede> ListarRede(TipoVeiculo? tipo);
        VwEntregadorRede? ObterNaRede(Guid id);
        List<Guid> ListarFavoritosIds();
        EmpresaFavorito? ObterFavorito(Guid entregadorId);
        void AdicionarFavorito(EmpresaFavorito favorito);
        void RemoverFavorito(EmpresaFavorito favorito);

        // Tela Entregadores da loja
        Dictionary<Guid, (long Entregues, long Falhas)> ContarEntregasDaLoja();
        HashSet<Guid> ListarComJornadaAberta();
        Dictionary<Guid, Guid> ListarEmpresaDaRotaAtual();
        Origem? ObterOrigemPadrao();

        // Tela "Meu perfil" do app
        List<DocumentoEntregador> ListarDocumentos(Guid entregadorId);
        VwEntregadorRede? ObterIndicadores(Guid entregadorId);
    }
}
