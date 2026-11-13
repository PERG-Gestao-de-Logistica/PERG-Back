using Perg.Api.Applications.Integracoes;
using Perg.Api.Applications.Services;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Eventos
{
    // Eventos de pedido da outbox viram envios para os webhooks da loja.
    // Aqui só entram na fila (webhook_entregas); quem envia e tenta de novo é o ProcessadorWebhooks.
    public class ConsumidorWebhooks : IConsumidorEvento
    {
        // Fila antiga (anterior ao webhook) não é despejada no sistema da loja
        private static readonly TimeSpan IdadeMaxima = TimeSpan.FromHours(1);

        private readonly IIntegracaoRepository _integracoes;
        private readonly IPedidoRepository _pedidos;
        private readonly IntegracaoService _servico;

        public ConsumidorWebhooks(IIntegracaoRepository integracoes, IPedidoRepository pedidos, IntegracaoService servico)
        {
            _integracoes = integracoes;
            _pedidos = pedidos;
            _servico = servico;
        }

        public string Nome => "webhooks-empresa";

        public bool Aceita(string topico, string tipoEvento) => topico == "pedido" && EventosWebhook.Nome(tipoEvento) != null;

        public void Processar(Outbox mensagem)
        {
            if (DateTime.UtcNow - mensagem.CriadoEm > IdadeMaxima) return;
            if (!Guid.TryParse(mensagem.Chave, out Guid pedidoId)) return;

            Pedido? pedido = _pedidos.ObterPorId(pedidoId);
            if (pedido == null) return;

            List<Webhook> webhooks = _integracoes.ListarWebhooksAtivosDaEmpresa(pedido.EmpresaId);
            if (webhooks.Count == 0) return;

            string evento = EventosWebhook.Nome(mensagem.TipoEvento)!;
            var dados = IntegracaoService.ParaDto(pedido, _servico.LinkRastreio(pedido));

            _integracoes.AdicionarEntregas(webhooks.Select(w => IntegracaoService.NovaEntrega(w, evento, dados)).ToList());
        }
    }
}
