namespace Perg.Api.Applications.Integracoes
{
    public static class StatusWebhookEntrega
    {
        public const string Pendente = "pendente";
        public const string Entregue = "entregue";
        public const string Falhou = "falhou";
    }

    // Nome público dos eventos (o que o sistema da loja recebe) a partir do tipo gravado na outbox
    public static class EventosWebhook
    {
        public static string? Nome(string tipoEvento) => tipoEvento switch
        {
            "PedidoCriado" => "pedido.criado",
            "PedidoAtualizado" => "pedido.atualizado",
            "PedidoLocalizacaoAjustada" => "pedido.atualizado",
            "PedidoRoteirizado" => "pedido.roteirizado",
            "PedidoSaiuParaEntrega" => "pedido.saiu_para_entrega",
            "PedidoEntregue" => "pedido.entregue",
            "OcorrenciaRegistrada" => "pedido.ocorrencia",
            "PedidoCancelado" => "pedido.cancelado",
            _ => null
        };
    }
}
