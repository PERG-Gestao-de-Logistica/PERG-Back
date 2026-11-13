using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IIntegracaoRepository
    {
        // Chaves de API
        ChaveApi? ObterChaveAtivaPorHash(string hash);
        void RegistrarUsoDaChave(ChaveApi chave);
        List<ChaveApi> ListarChaves();
        ChaveApi? ObterChave(Guid id);
        void AdicionarChave(ChaveApi chave);

        // Webhooks (filtrados pela empresa logada)
        List<Webhook> ListarWebhooks();
        Webhook? ObterWebhook(Guid id);
        void AdicionarWebhook(Webhook webhook);
        void RemoverWebhook(Webhook webhook);
        List<WebhookEntrega> ListarEntregas(Guid webhookId, int quantidade);

        // Fila de envio (segundo plano, sem filtro de empresa)
        List<Webhook> ListarWebhooksAtivosDaEmpresa(Guid empresaId);
        void AdicionarEntregas(IEnumerable<WebhookEntrega> entregas);
        List<WebhookEntrega> ListarEntregasParaEnviar(int quantidade);

        void Salvar();
    }
}
