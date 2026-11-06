using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Services;

namespace Perg.Api.Controllers
{
    // Chamadas que o Asaas faz para a API. Sem login: a autenticação é o cabeçalho "asaas-access-token",
    // que precisa ser igual a Asaas:WebhookToken (Segredos do Usuário).
    [Route("api/webhooks/asaas")]
    [ApiController]
    public class WebhooksAsaasController : ControllerBase
    {
        private readonly SaqueService _service;
        private readonly IConfiguration _config;

        public WebhooksAsaasController(SaqueService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }

        // Eventos de transferência (configurar no painel do Asaas: Integrações > Webhooks)
        [HttpPost]
        public ActionResult ReceberEvento([FromBody] JsonElement corpo)
        {
            if (!TokenValido())
            {
                return Unauthorized();
            }

            string evento = corpo.TryGetProperty("event", out var e) ? e.GetString() ?? "" : "";
            if (evento.StartsWith("TRANSFER_") && corpo.TryGetProperty("transfer", out var transferencia))
            {
                _service.ProcessarEventoTransferencia(
                    evento,
                    Texto(transferencia, "id"),
                    Texto(transferencia, "externalReference"),
                    Texto(transferencia, "failReason"));
            }

            // 200 sempre que o evento foi recebido; erro aqui faria o Asaas reenviar
            return Ok();
        }

        // Mecanismo de validação de saque (opcional no Asaas): aprova só transferências que nasceram de um saque nosso
        [HttpPost("validacao-saque")]
        public ActionResult ValidarSaque([FromBody] JsonElement corpo)
        {
            if (!TokenValido())
            {
                return Unauthorized();
            }

            if (!corpo.TryGetProperty("transfer", out var transferencia))
            {
                return Ok(new { status = "REFUSED", refuseReason = "Requisição sem transferência." });
            }

            decimal valor = transferencia.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : -1;
            bool aprovado = _service.AprovarTransferencia(Texto(transferencia, "id"), Texto(transferencia, "externalReference"), valor);

            return Ok(aprovado
                ? new { status = "APPROVED", refuseReason = (string?)null }
                : new { status = "REFUSED", refuseReason = (string?)"Transferência não reconhecida pelo Perg." });
        }

        private bool TokenValido()
        {
            string? esperado = _config["Asaas:WebhookToken"];
            string? recebido = Request.Headers["asaas-access-token"].FirstOrDefault();

            if (string.IsNullOrEmpty(esperado) || string.IsNullOrEmpty(recebido))
            {
                return false;
            }

            // Comparação em tempo constante (não revela o token por tempo de resposta)
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(recebido));
        }

        private static string? Texto(JsonElement objeto, string propriedade)
        {
            return objeto.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
        }
    }
}
