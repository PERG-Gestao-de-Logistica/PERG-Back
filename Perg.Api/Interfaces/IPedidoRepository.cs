using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IPedidoRepository
    {
        // situacao: abas da tela (prontos/problemas dependem de agora e do fim do dia da loja, em UTC)
        (List<Pedido> Itens, int Total) Listar(StatusPedido? status, string? situacao, DateTime agoraUtc, DateTime fimDoDiaUtc,
            DateTime? criadoDesde, DateTime? criadoAte, string? busca, int pagina, int tamanhoPagina);

        // Versão antiga (sem as abas), mantida para quem já chamava
        (List<Pedido> Itens, int Total) Listar(StatusPedido? status, DateTime? criadoDesde, DateTime? criadoAte,
            string? busca, int pagina, int tamanhoPagina);

        // Quantidade de pedidos em cada aba da tela
        Dictionary<string, int> Contar(DateTime agoraUtc, DateTime fimDoDiaUtc);

        Pedido? ObterPorId(Guid id);
        Pedido? ObterPorToken(string token);

        // Pedido + evento da linha do tempo + evento da outbox num único SaveChanges (mesma transação)
        void Adicionar(Pedido pedido, PedidoEvento evento, Outbox mensagem);
        void Atualizar(Pedido pedido, PedidoEvento evento, Outbox mensagem);

        // Integração: false = outro envio com o mesmo id_externo gravou antes (índice ux_pedidos_externo)
        bool TentarAdicionar(Pedido pedido, PedidoEvento evento, Outbox mensagem);

        // Importação: o mesmo pedido da loja (id_externo) não entra duas vezes
        bool ExisteIdExterno(CanalPedido canal, string idExterno);

        Pedido? ObterPorIdExterno(CanalPedido canal, string idExterno);
        List<Pedido> ListarAtualizadosDesde(DateTime desdeUtc, int quantidade);

        // Alteração sem evento na linha do tempo (ex.: geocodificação automática)
        void Atualizar(Pedido pedido);
    }
}
