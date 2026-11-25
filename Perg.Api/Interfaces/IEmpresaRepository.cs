using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IEmpresaRepository
    {
        bool CnpjExiste(string cnpj);

        Empresa? ObterPorId(Guid id);

        // Grava empresa, admin e origem padrão juntos (um único SaveChanges = uma transação)
        void Cadastrar(Empresa empresa, Usuario admin, Origem origem);

        // Grava as alterações de uma empresa já carregada por ObterPorId
        void Atualizar(Empresa empresa);
    }
}
