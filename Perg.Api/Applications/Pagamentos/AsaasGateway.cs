using System.Net.Http.Json;
using System.Text.Json;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Pagamentos
{
    // Asaas: POST /transfers (Pix para chave). Documentação: https://docs.asaas.com
    // Configuração: Asaas:BaseUrl (appsettings) e Asaas:ApiKey (Segredos do Usuário).
    public class AsaasGateway : IGatewayPagamento
    {
        private readonly HttpClient _http;
        private readonly ILogger<AsaasGateway> _logger;

        public AsaasGateway(HttpClient http, ILogger<AsaasGateway> logger)
        {
            _http = http;
            _logger = logger;
        }

        public ResultadoTransferencia TransferirPix(Guid saqueId, decimal valor, ChavePixNormalizada chave, string descricao)
        {
            var corpo = new
            {
                value = valor,
                pixAddressKey = chave.Valor,
                pixAddressKeyType = chave.Tipo.ToString().ToUpperInvariant(),
                description = descricao,
                // Liga a transferência ao nosso saque (usado no webhook)
                externalReference = saqueId.ToString()
            };

            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Post, "transfers") { Content = JsonContent.Create(corpo) };
                using HttpResponseMessage resposta = _http.Send(requisicao);
                using var leitor = new StreamReader(resposta.Content.ReadAsStream());
                string texto = leitor.ReadToEnd();

                if (!resposta.IsSuccessStatusCode)
                {
                    string erro = LerErro(texto) ?? $"Asaas respondeu {(int)resposta.StatusCode}.";
                    _logger.LogWarning("Asaas recusou o saque {Saque}: {Erro}", saqueId, erro);
                    return new ResultadoTransferencia(SituacaoTransferencia.Falhou, null, erro);
                }

                using JsonDocument json = JsonDocument.Parse(texto);
                string? id = json.RootElement.TryGetProperty("id", out var idJson) ? idJson.GetString() : null;
                string status = json.RootElement.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                SituacaoTransferencia situacao = status switch
                {
                    "DONE" => SituacaoTransferencia.Concluida,
                    "FAILED" or "CANCELLED" => SituacaoTransferencia.Falhou,
                    _ => SituacaoTransferencia.Processando // PENDING, BANK_PROCESSING, SCHEDULED...: o webhook avisa o fim
                };

                return new ResultadoTransferencia(situacao, id, situacao == SituacaoTransferencia.Falhou ? $"Transferência {status}." : null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Não sabemos se o Asaas recebeu: fica "processando" e o webhook (ou uma conferência manual) decide
                _logger.LogError(ex, "Falha de comunicação com o Asaas no saque {Saque}.", saqueId);
                return new ResultadoTransferencia(SituacaoTransferencia.Processando, null, "Sem resposta do Asaas.");
            }
        }

        public void CancelarTransferencia(string idNoGateway)
        {
            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Delete, $"transfers/{Uri.EscapeDataString(idNoGateway)}/cancel");
                using HttpResponseMessage resposta = _http.Send(requisicao);
                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Asaas não cancelou a transferência {Id} ({Status}).", idNoGateway, (int)resposta.StatusCode);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Falha ao cancelar a transferência {Id} no Asaas.", idNoGateway);
            }
        }

        // Erros do Asaas vêm como { "errors": [ { "code": "...", "description": "..." } ] }
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
