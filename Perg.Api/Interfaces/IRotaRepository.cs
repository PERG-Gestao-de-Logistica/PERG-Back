using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IRotaRepository
    {
        List<Rota> ListarPorData(DateOnly dataOperacao);
        Rota? ObterPorId(Guid id);
        void Atualizar(List<PedidoEvento> eventos, List<Outbox> mensagens);
    }
}
