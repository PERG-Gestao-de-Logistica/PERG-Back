using System.Text.Json;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Geocodificacao
{
    // Google Geocoding API: https://developers.google.com/maps/documentation/geocoding
    public class GoogleGeocodificador : IGeocodificador
    {
        private const string Url = "https://maps.googleapis.com/maps/api/geocode/json";

        private readonly HttpClient _http;
        private readonly string? _chave;
        private readonly ILogger<GoogleGeocodificador> _logger;

        public GoogleGeocodificador(HttpClient http, IConfiguration config, ILogger<GoogleGeocodificador> logger)
        {
            _http = http;
            _chave = config["Google:GeocodingApiKey"];
            _logger = logger;
        }

        public ResultadoGeocodificacao Geocodificar(EnderecoGeocodificacao endereco)
        {
            if (string.IsNullOrWhiteSpace(_chave))
            {
                _logger.LogWarning("Geocodificação ignorada: Google:GeocodingApiKey não configurada.");
                return ResultadoGeocodificacao.Pendente();
            }

            string enderecoTexto = Uri.EscapeDataString(endereco.ParaTexto());
            string url = $"{Url}?address={enderecoTexto}&region=br&language=pt-BR&components=country:BR&key={_chave}";

            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
                using HttpResponseMessage resposta = _http.Send(requisicao);
                using Stream corpo = resposta.Content.ReadAsStream();
                using JsonDocument json = JsonDocument.Parse(corpo);

                string status = json.RootElement.GetProperty("status").GetString() ?? "";

                if (status == "ZERO_RESULTS")
                {
                    return new ResultadoGeocodificacao { Status = StatusGeocodificacao.Falhou };
                }

                if (status != "OK")
                {
                    // REQUEST_DENIED (chave errada), OVER_QUERY_LIMIT, etc.: não é culpa do endereço
                    string? erro = json.RootElement.TryGetProperty("error_message", out var mensagem) ? mensagem.GetString() : null;
                    _logger.LogWarning("Google Geocoding respondeu {Status}: {Erro}", status, erro);
                    return ResultadoGeocodificacao.Pendente();
                }

                JsonElement primeiro = json.RootElement.GetProperty("results")[0];
                JsonElement geometria = primeiro.GetProperty("geometry");
                JsonElement local = geometria.GetProperty("location");

                string tipoLocal = geometria.GetProperty("location_type").GetString() ?? "";
                bool parcial = primeiro.TryGetProperty("partial_match", out var p) && p.GetBoolean();

                decimal confianca = CalcularConfianca(tipoLocal, parcial);

                return new ResultadoGeocodificacao
                {
                    Latitude = Math.Round(local.GetProperty("lat").GetDecimal(), 6),
                    Longitude = Math.Round(local.GetProperty("lng").GetDecimal(), 6),
                    Confianca = confianca,
                    // Abaixo de 0.8 o ponto pode estar longe da porta: a loja confere no mapa
                    Status = confianca >= 0.8m ? StatusGeocodificacao.Ok : StatusGeocodificacao.Revisar
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Falha ao consultar o Google Geocoding.");
                return ResultadoGeocodificacao.Pendente();
            }
        }

        // ROOFTOP = número exato; RANGE_INTERPOLATED = estimado na rua;
        // GEOMETRIC_CENTER = centro da rua/bairro; APPROXIMATE = só a região
        private static decimal CalcularConfianca(string tipoLocal, bool parcial)
        {
            decimal confianca = tipoLocal switch
            {
                "ROOFTOP" => 1.00m,
                "RANGE_INTERPOLATED" => 0.85m,
                "GEOMETRIC_CENTER" => 0.50m,
                _ => 0.30m
            };

            // partial_match: o Google não achou o endereço inteiro, só parte dele
            if (parcial)
            {
                confianca *= 0.7m;
            }

            return Math.Round(confianca, 2);
        }
    }
}
