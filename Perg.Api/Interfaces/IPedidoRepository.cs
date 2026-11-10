using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IPedidoRepository
    {
        (List<Pedido> Itens, int Total) Listar(StatusPedido? status, DateTime? criadoDesde, DateTime? criadoAte,
            string? busca, int pagina, int tamanhoPagina);

        Pedido? ObterPorId(Guid id);
        Pedido? ObterPorToken(string token);

        // Pedido + evento da linha do tempo + evento da outbox num único SaveChanges (mesma transação)
        void Adicionar(Pedido pedido, PedidoEvento evento, Outbox mensagem);
        void Atualizar(Pedido pedido, PedidoEvento evento, Outbox mensagem);

        // Importação: o mesmo pedido da loja (id_externo) não entra duas vezes
        bool ExisteIdExterno(CanalPedido canal, string idExterno);

        // Alteração sem evento na linha do tempo (ex.: geocodificação automática)
        void Atualizar(Pedido pedido);
    }
}
