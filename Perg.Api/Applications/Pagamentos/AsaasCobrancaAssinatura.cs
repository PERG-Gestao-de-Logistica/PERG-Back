using System.Net.Http.Json;
using System.Text.Json;
using Perg.Api.Domains;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Pagamentos
{
    // Asaas: cliente (pelo CNPJ da loja) + assinatura mensal (POST /subscriptions).
    // A forma de pagamento fica em aberto (UNDEFINED): a loja escolhe boleto, Pix ou cartão na fatura.
    public class AsaasCobrancaAssinatura : ICobrancaAssinatura
    {
        private readonly HttpClient _http;
        private readonly ILogger<AsaasCobrancaAssinatura> _logger;

        public AsaasCobrancaAssinatura(HttpClient http, ILogger<AsaasCobrancaAssinatura> logger)
        {
            _http = http;
            _logger = logger;
        }

        public AssinaturaNoGateway Criar(Empresa empresa, Plano plano, Guid assinaturaId)
        {
            string clienteId = ObterOuCriarCliente(empresa);
            string hoje = DateTime.UtcNow.ToString("yyyy-MM-dd");

            JsonElement assinatura = Enviar(HttpMethod.Post, "subscriptions", new
            {
                customer = clienteId,
                billingType = "UNDEFINED",
                value = plano.PrecoMensal,
                nextDueDate = hoje,
                cycle = "MONTHLY",
                description = $"Perg - plano {plano.Nome}",
                externalReference = assinaturaId.ToString()
            });

            string id = assinatura.GetProperty("id").GetString()!;
            return new AssinaturaNoGateway("asaas", id, ObterLinkPendente(id), DateOnly.FromDateTime(DateTime.UtcNow));
        }

        public void Cancelar(string idNoGateway)
        {
            try
            {
                Enviar(HttpMethod.Delete, $"subscriptions/{Uri.EscapeDataString(idNoGateway)}", null);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning("Asaas não cancelou a assinatura {Id}: {Erro}", idNoGateway, ex.Message);
            }
        }

        public string? ObterLinkPendente(string idNoGateway)
        {
            JsonElement? pagamento = PrimeiroPagamentoEmAberto(idNoGateway);
            return pagamento?.TryGetProperty("invoiceUrl", out var link) == true ? link.GetString() : null;
        }

        public bool MarcarPendenteComoPaga(string idNoGateway)
        {
            JsonElement? pagamento = PrimeiroPagamentoEmAberto(idNoGateway);
            if (pagamento == null) return false;

            string pagamentoId = pagamento.Value.GetProperty("id").GetString()!;
            decimal valor = pagamento.Value.GetProperty("value").GetDecimal();

            Enviar(HttpMethod.Post, $"payments/{pagamentoId}/receiveInCash", new
            {
                paymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                value = valor
            });
            return true;
        }

        // ---------- apoio ----------

        private JsonElement? PrimeiroPagamentoEmAberto(string idNoGateway)
        {
            JsonElement lista = Enviar(HttpMethod.Get, $"subscriptions/{Uri.EscapeDataString(idNoGateway)}/payments", null);
            foreach (JsonElement pagamento in lista.GetProperty("data").EnumerateArray())
            {
                string status = pagamento.GetProperty("status").GetString() ?? "";
                if (status is "PENDING" or "OVERDUE")
                {
                    return pagamento.Clone();
                }
            }
            return null;
        }

        private string ObterOuCriarCliente(Empresa empresa)
        {
            JsonElement busca = Enviar(HttpMethod.Get, $"customers?cpfCnpj={Uri.EscapeDataString(empresa.Cnpj)}", null);
            JsonElement dados = busca.GetProperty("data");
            if (dados.GetArrayLength() > 0)
            {
                return dados[0].GetProperty("id").GetString()!;
            }

            JsonElement cliente = Enviar(HttpMethod.Post, "customers", new
            {
                name = empresa.RazaoSocial,
                cpfCnpj = empresa.Cnpj,
                email = empresa.Email,
                externalReference = empresa.Id.ToString(),
                notificationDisabled = true // os avisos de cobrança saem pelo Perg
            });
            return cliente.GetProperty("id").GetString()!;
        }

        // Erro do Asaas vira DomainException com a mensagem dele (ex.: "CPF/CNPJ inválido")
        private JsonElement Enviar(HttpMethod metodo, string caminho, object? corpo)
        {
            try
            {
                using var requisicao = new HttpRequestMessage(metodo, caminho);
                if (corpo != null) requisicao.Content = JsonContent.Create(corpo);

                using HttpResponseMessage resposta = _http.Send(requisicao);
                using var leitor = new StreamReader(resposta.Content.ReadAsStream());
                string texto = leitor.ReadToEnd();

                if (!resposta.IsSuccessStatusCode)
                {
                    string erro = LerErro(texto) ?? $"Asaas respondeu {(int)resposta.StatusCode}.";
                    _logger.LogWarning("Asaas {Metodo} {Caminho}: {Erro}", metodo, caminho, erro);
                    throw new DomainException("Cobrança: " + erro);
                }

                using JsonDocument json = JsonDocument.Parse(string.IsNullOrWhiteSpace(texto) ? "{}" : texto);
                return json.RootElement.Clone();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Sem resposta do Asaas em {Caminho}.", caminho);
                throw new DomainException("Não foi possível falar com o sistema de cobrança agora. Tente de novo em instantes.");
            }
        }

        private static string? LerErro(string texto)
        {
            try
            {
                using JsonDocument json = JsonDocument.Parse(texto);
                if (json.RootElement.TryGetProperty("errors", out var erros) && erros.GetArrayLength() > 0)
                {
                    return erros[0].GetProperty("description").GetString();
                }
            }
            catch (JsonException) { }
            return null;
        }
    }
}
