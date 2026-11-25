using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IOrigemRepository
    {
        List<Origem> Listar();
        Origem? ObterPorId(Guid id);
        Origem? ObterPadrao();
        void Adicionar(Origem origem);

        // Grava as alterações de uma origem já carregada; se ela virou padrão, desmarca a anterior
        void Atualizar(Origem origem);
    }
}
