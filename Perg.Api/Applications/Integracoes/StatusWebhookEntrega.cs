namespace Perg.Api.Applications.Integracoes
{
    public static class StatusWebhookEntrega
    {
        public const string Pendente = "pendente";
        public const string Entregue = "entregue";
        public const string Falhou = "falhou";
    }

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
