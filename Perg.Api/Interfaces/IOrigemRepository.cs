using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IOrigemRepository
    {
        List<Origem> Listar();
        Origem? ObterPorId(Guid id);
        Origem? ObterPadrao();
        void Adicionar(Origem origem);
    }
}
