using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IRoteirizacaoRepository
    {
        // Pedidos da origem com status novo, localização ok e que não são retirada
        List<Pedido> ListarPedidosProntos(Guid origemId);
        List<Pedido> ObterPedidos(List<Guid> ids);

        // Próximo número de rota do dia (R01, R02...)
        short ProximoNumeroRota(DateOnly dataOperacao);

        Roteirizacao? ObterPorId(Guid id);

        // Roteirização + rotas + paradas + pedidos alterados + eventos num único SaveChanges
        void Adicionar(Roteirizacao roteirizacao, List<Rota> rotas, List<PedidoEvento> eventos, List<Outbox> mensagens);
    }
}
