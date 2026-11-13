using System.Security.Cryptography;
using System.Text;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Integracoes
{
    // Envia a fila de webhooks a cada 5 s. POST com o JSON do evento e os cabeçalhos:
    //   X-Perg-Evento, X-Perg-Entrega (id, para o sistema ignorar repetidos) e
    //   X-Perg-Assinatura: sha256=<HMAC-SHA256 do corpo com o segredo do webhook>.
    // Resposta 2xx = entregue. Falha = nova tentativa em 1 min, 5 min, 30 min, 2 h, 6 h e 12 h; depois "falhou".
    // 20 falhas seguidas desativam o webhook e avisam a loja no sino.
    public class ProcessadorWebhooks : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan[] Esperas =
        [
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(2), TimeSpan.FromHours(6), TimeSpan.FromHours(12)
        ];
        private const int LimiteFalhasSeguidas = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ProcessadorWebhooks> _logger;

        public ProcessadorWebhooks(IServiceScopeFactory scopeFactory, IHttpClientFactory http, ILogger<ProcessadorWebhooks> logger)
        {
            _scopeFactory = scopeFactory;
            _http = http;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnviarLote(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Falha no envio de webhooks.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }

        private async Task EnviarLote(CancellationToken parar)
        {
            using IServiceScope escopo = _scopeFactory.CreateScope();
            var repository = escopo.ServiceProvider.GetRequiredService<IIntegracaoRepository>();

            foreach (WebhookEntrega entrega in repository.ListarEntregasParaEnviar(20))
            {
                if (parar.IsCancellationRequested) break;

                Webhook webhook = entrega.Webhook;
                if (!webhook.Ativo && entrega.Evento != "ping")
                {
                    entrega.Status = StatusWebhookEntrega.Falhou;
                    entrega.UltimoErro = "Webhook desativado.";
                    continue;
                }

                (bool ok, int? statusHttp, string? erro) = await Enviar(webhook, entrega, parar);

                entrega.Tentativas++;
                entrega.UltimoStatusHttp = statusHttp;
                entrega.UltimoErro = erro;

                if (ok)
                {
                    entrega.Status = StatusWebhookEntrega.Entregue;
                    entrega.EntregueEm = DateTime.UtcNow;
                    webhook.FalhasSeguidas = 0;
                    continue;
                }

                webhook.FalhasSeguidas++;
                if (entrega.Tentativas > Esperas.Length)
                {
                    entrega.Status = StatusWebhookEntrega.Falhou;
                }
                else
                {
                    entrega.ProximaTentativaEm = DateTime.UtcNow + Esperas[entrega.Tentativas - 1];
                }

                if (webhook.Ativo && webhook.FalhasSeguidas >= LimiteFalhasSeguidas)
                {
                    webhook.Ativo = false;
                    AvisarDesativacao(escopo, webhook);
                }
            }

            repository.Salvar();
        }

        private async Task<(bool Ok, int? StatusHttp, string? Erro)> Enviar(Webhook webhook, WebhookEntrega entrega, CancellationToken parar)
        {
            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(entrega.Payload, Encoding.UTF8, "application/json")
                };
                requisicao.Headers.Add("X-Perg-Evento", entrega.Evento);
                requisicao.Headers.Add("X-Perg-Entrega", entrega.Id.ToString());
                requisicao.Headers.Add("X-Perg-Assinatura", "sha256=" + Assinar(webhook.Segredo, entrega.Payload));

                HttpClient cliente = _http.CreateClient("webhooks");
                using HttpResponseMessage resposta = await cliente.SendAsync(requisicao, parar);
                int status = (int)resposta.StatusCode;
                return resposta.IsSuccessStatusCode ? (true, status, null) : (false, status, $"Respondeu HTTP {status}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !parar.IsCancellationRequested)
            {
                return (false, null, ex is TaskCanceledException ? "Sem resposta em 10 segundos." : ex.Message);
            }
        }

        public static string Assinar(string segredo, string corpo) =>
            Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo))).ToLowerInvariant();

        private static void AvisarDesativacao(IServiceScope escopo, Webhook webhook)
        {
            var notificacoes = escopo.ServiceProvider.GetRequiredService<IAssinaturaRepository>();
            foreach (Guid usuarioId in notificacoes.ListarUsuariosParaCobranca(webhook.EmpresaId))
            {
                notificacoes.AdicionarNotificacao(new Notificacao
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = webhook.EmpresaId,
                    UsuarioId = usuarioId,
                    Tipo = Domains.Enums.TipoNotificacao.Sistema,
                    Titulo = "Webhook desativado após 20 falhas seguidas. Confira o endereço em Integrações.",
                    Link = "/configuracoes/integracoes",
                    CriadoEm = DateTime.UtcNow
                });
            }
        }
    }
}
