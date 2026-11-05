using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Geocodificacao
{
    // Nominatim (OpenStreetMap), servidor público e gratuito.
    //
    // IMPORTANTE: o uso do servidor público tem regras (https://operations.osmfoundation.org/policies/nominatim/):
    //  - no máximo 1 consulta por segundo (este código garante isso);
    //  - User-Agent identificando o app (configurado abaixo);
    //  - resultados em cache (memória aqui + coordenadas gravadas no pedido);
    //  - atribuição visível "© OpenStreetMap contributors" no front onde aparecer o mapa;
    //  - proibido autocomplete e desencorajado geocodificar em massa (importação de planilha).
    // Para produção com muitos clientes, o caminho é hospedar o próprio Nominatim ou trocar de provedor
    // (basta mudar Geocodificacao:Provedor no appsettings).
    public class NominatimGeocodificador : IGeocodificador
    {
        private const string UrlPadrao = "https://nominatim.openstreetmap.org/search";

        // Trava compartilhada por toda a API para respeitar 1 consulta/segundo
        private static readonly object Trava = new();
        private static DateTime _ultimaConsulta = DateTime.MinValue;

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NominatimGeocodificador> _logger;
        private readonly string _url;

        public NominatimGeocodificador(HttpClient http, IMemoryCache cache, IConfiguration config, ILogger<NominatimGeocodificador> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
            _url = config["Geocodificacao:NominatimUrl"] ?? UrlPadrao;
        }

        public ResultadoGeocodificacao Geocodificar(EnderecoGeocodificacao endereco)
        {
            ResultadoGeocodificacao resultado = Consultar(MontarUrl(endereco, usarCep: true));

            // Muitas ruas no OpenStreetMap não têm CEP cadastrado: tenta de novo sem ele
            if (resultado.Status == StatusGeocodificacao.Falhou && !string.IsNullOrWhiteSpace(endereco.Cep))
            {
                resultado = Consultar(MontarUrl(endereco, usarCep: false));
            }

            return resultado;
        }

        private ResultadoGeocodificacao Consultar(string url)
        {
            if (_cache.TryGetValue(url, out ResultadoGeocodificacao? emCache) && emCache != null)
            {
                return emCache;
            }

            try
            {
                string corpo;
                lock (Trava)
                {
                    TimeSpan desdeUltima = DateTime.UtcNow - _ultimaConsulta;
                    if (desdeUltima < TimeSpan.FromSeconds(1.1))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1.1) - desdeUltima);
                    }

                    using var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
                    using HttpResponseMessage resposta = _http.Send(requisicao);
                    _ultimaConsulta = DateTime.UtcNow;

                    if (!resposta.IsSuccessStatusCode)
                    {
                        // 429 = passou do limite; 403 = bloqueado. Não é culpa do endereço.
                        _logger.LogWarning("Nominatim respondeu {Status}.", (int)resposta.StatusCode);
                        return ResultadoGeocodificacao.Pendente();
                    }

                    using var leitor = new StreamReader(resposta.Content.ReadAsStream());
                    corpo = leitor.ReadToEnd();
                }

                ResultadoGeocodificacao resultado = Interpretar(corpo);

                // Guarda em memória por 1 dia (o mesmo endereço não vai de novo ao servidor)
                if (resultado.Status != StatusGeocodificacao.Pendente)
                {
                    _cache.Set(url, resultado, TimeSpan.FromDays(1));
                }

                return resultado;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                _logger.LogWarning(ex, "Falha ao consultar o Nominatim.");
                return ResultadoGeocodificacao.Pendente();
            }
        }

        // Busca estruturada: funciona melhor que o texto livre para endereços brasileiros
        private string MontarUrl(EnderecoGeocodificacao endereco, bool usarCep)
        {
            var parametros = new List<string>
            {
                "format=jsonv2",
                "limit=1",
                "addressdetails=1",
                "countrycodes=br",
                "accept-language=pt-BR"
            };

            string rua = string.Join(" ", new[] { endereco.Numero, endereco.Logradouro }.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (!string.IsNullOrWhiteSpace(rua)) parametros.Add("street=" + Uri.EscapeDataString(rua));
            if (!string.IsNullOrWhiteSpace(endereco.Cidade)) parametros.Add("city=" + Uri.EscapeDataString(endereco.Cidade));
            if (!string.IsNullOrWhiteSpace(endereco.Uf)) parametros.Add("state=" + Uri.EscapeDataString(endereco.Uf));
            if (usarCep && !string.IsNullOrWhiteSpace(endereco.Cep)) parametros.Add("postalcode=" + Uri.EscapeDataString(endereco.Cep));

            return _url + "?" + string.Join("&", parametros);
        }

        private static ResultadoGeocodificacao Interpretar(string corpo)
        {
            using JsonDocument json = JsonDocument.Parse(corpo);

            if (json.RootElement.GetArrayLength() == 0)
            {
                return new ResultadoGeocodificacao { Status = StatusGeocodificacao.Falhou };
            }

            JsonElement primeiro = json.RootElement[0];

            decimal latitude = decimal.Parse(primeiro.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            decimal longitude = decimal.Parse(primeiro.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);

            // place_rank 30 = imóvel/número; 26-29 = rua; abaixo disso = bairro/cidade
            int rank = primeiro.TryGetProperty("place_rank", out var r) ? r.GetInt32() : 0;
            bool achouNumero = primeiro.TryGetProperty("address", out var endereco)
                && endereco.TryGetProperty("house_number", out _);

            decimal confianca = achouNumero || rank >= 30 ? 1.00m
                : rank >= 26 ? 0.60m
                : 0.30m;

            return new ResultadoGeocodificacao
            {
                Latitude = Math.Round(latitude, 6),
                Longitude = Math.Round(longitude, 6),
                Confianca = confianca,
                // Achou só a rua ou o bairro: a loja confere o pino no mapa
                Status = confianca >= 0.8m ? StatusGeocodificacao.Ok : StatusGeocodificacao.Revisar
            };
        }
    }
}
