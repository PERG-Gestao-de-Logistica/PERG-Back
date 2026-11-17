using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IRoteirizacaoRepository
    {
        List<Pedido> ListarPedidosProntos(Guid origemId);
        List<Pedido> ObterPedidos(List<Guid> ids);

        short ProximoNumeroRota(DateOnly dataOperacao);

        Roteirizacao? ObterPorId(Guid id);

        // Roteirização + rotas + paradas + pedidos alterados + eventos num único SaveChanges
        void Adicionar(Roteirizacao roteirizacao, List<Rota> rotas, List<PedidoEvento> eventos, List<Outbox> mensagens);
    }
}
