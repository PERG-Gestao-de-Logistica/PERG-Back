using System.Text;
using System.Text.Json;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Sms
{
    // Envio de SMS pelo Twilio (POST /Accounts/{sid}/Messages.json). Documentação: https://www.twilio.com/docs/messaging/api/message-resource
    // Configuração (Segredos do Usuário): Twilio:AccountSid, Twilio:AuthToken, Twilio:NumeroRemetente (+1...).
    // Em desenvolvimento, Twilio:RedirecionarPara manda todo SMS para um celular verificado na conta de teste
    // (a conta trial só envia para números verificados).
    public class TwilioEnvioSms : IEnvioSms
    {
        private readonly HttpClient _http;
        private readonly ILogger<TwilioEnvioSms> _logger;
        private readonly string _accountSid;
        private readonly string _numeroRemetente;
        private readonly string? _redirecionarPara;

        public TwilioEnvioSms(HttpClient http, IConfiguration configuracao, ILogger<TwilioEnvioSms> logger)
        {
            _http = http;
            _logger = logger;
            _accountSid = configuracao["Twilio:AccountSid"]!;
            _numeroRemetente = configuracao["Twilio:NumeroRemetente"]!;
            _redirecionarPara = configuracao["Twilio:RedirecionarPara"];

            string credenciais = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{configuracao["Twilio:AuthToken"]}"));
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credenciais);
        }

        public bool Enviar(string telefone, string mensagem)
        {
            string destino = string.IsNullOrWhiteSpace(_redirecionarPara) ? "+55" + telefone : _redirecionarPara;

            var campos = new Dictionary<string, string>
            {
                ["To"] = destino,
                ["From"] = _numeroRemetente,
                ["Body"] = mensagem
            };

            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Post, $"Accounts/{_accountSid}/Messages.json")
                {
                    Content = new FormUrlEncodedContent(campos)
                };
                using HttpResponseMessage resposta = _http.Send(requisicao);
                using var leitor = new StreamReader(resposta.Content.ReadAsStream());
                string texto = leitor.ReadToEnd();

                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Twilio recusou o SMS para {Destino}: {Erro}", Mascarar(destino), LerErro(texto) ?? $"status {(int)resposta.StatusCode}");
                    return false;
                }

                if (destino != "+55" + telefone)
                {
                    _logger.LogInformation("SMS para +55{Telefone} redirecionado para {Destino} (Twilio:RedirecionarPara).", telefone, Mascarar(destino));
                }

                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Falha de comunicação com o Twilio.");
                return false;
            }
        }

        // Erro do Twilio: { "code": 21608, "message": "...", "more_info": "..." }
        private static string? LerErro(string texto)
        {
            try
            {
                using JsonDocument json = JsonDocument.Parse(texto);
                string? mensagem = json.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
                string? codigo = json.RootElement.TryGetProperty("code", out var c) ? c.ToString() : null;
                return mensagem == null ? null : $"{mensagem} (código {codigo})";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Não grava o número inteiro no log
        private static string Mascarar(string numero) => numero.Length > 4 ? new string('*', numero.Length - 4) + numero[^4..] : numero;
    }
}
