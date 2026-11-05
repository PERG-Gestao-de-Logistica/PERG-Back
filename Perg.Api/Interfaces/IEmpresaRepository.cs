using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IEmpresaRepository
    {
        bool CnpjExiste(string cnpj);

        // Grava empresa, admin e origem padrão juntos (um único SaveChanges = uma transação)
        void Cadastrar(Empresa empresa, Usuario admin, Origem origem);
    }
}
