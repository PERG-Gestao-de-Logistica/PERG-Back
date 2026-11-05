using System.Globalization;
using System.Text.Json;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Roteamento
{
    // OSRM: distâncias e tempos reais pelas ruas (dados do OpenStreetMap).
    //
    // O servidor público (router.project-osrm.org) é só para demonstração: no máximo 1 consulta por segundo
    // e sem uso pesado (https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server).
    // Para produção, hospedar o próprio OSRM e trocar Roteirizacao:OsrmUrl no appsettings.
    // Se o OSRM falhar, cai para uma estimativa em linha reta para a roteirização não parar.
    public class OsrmMatrizDistancias : IMatrizDistancias
    {
        private const string UrlPadrao = "https://router.project-osrm.org";
        private const int MaximoPontosTabela = 100;

        private static readonly object Trava = new();
        private static DateTime _ultimaConsulta = DateTime.MinValue;

        private readonly HttpClient _http;
        private readonly ILogger<OsrmMatrizDistancias> _logger;
        private readonly string _url;

        public OsrmMatrizDistancias(HttpClient http, IConfiguration config, ILogger<OsrmMatrizDistancias> logger)
        {
            _http = http;
            _logger = logger;
            _url = (config["Roteirizacao:OsrmUrl"] ?? UrlPadrao).TrimEnd('/');
        }

        public MatrizDistancias Calcular(List<PontoGeografico> pontos)
        {
            if (pontos.Count <= MaximoPontosTabela)
            {
                try
                {
                    string url = $"{_url}/table/v1/driving/{Coordenadas(pontos)}?annotations=duration,distance";
                    using JsonDocument json = Consultar(url);

                    if (json.RootElement.GetProperty("code").GetString() == "Ok")
                    {
                        int n = pontos.Count;
                        var matriz = new MatrizDistancias { Metros = new long[n, n], Segundos = new long[n, n] };
                        JsonElement distancias = json.RootElement.GetProperty("distances");
                        JsonElement duracoes = json.RootElement.GetProperty("durations");

                        for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                JsonElement d = distancias[i][j];
                                JsonElement t = duracoes[i][j];
                                // null = OSRM não achou caminho entre os pontos (ilha, rua sem ligação)
                                matriz.Metros[i, j] = d.ValueKind == JsonValueKind.Null ? 1_000_000 : (long)Math.Round(d.GetDouble());
                                matriz.Segundos[i, j] = t.ValueKind == JsonValueKind.Null ? 86_400 : (long)Math.Round(t.GetDouble());
                            }
                        }

                        return matriz;
                    }

                    _logger.LogWarning("OSRM respondeu {Codigo}. Usando estimativa em linha reta.", json.RootElement.GetProperty("code").GetString());
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
                {
                    _logger.LogWarning(ex, "Falha ao consultar o OSRM. Usando estimativa em linha reta.");
                }
            }

            return EstimativaLinhaReta.Calcular(pontos);
        }

        public string? Trajeto(List<PontoGeografico> pontosEmOrdem)
        {
            try
            {
                string url = $"{_url}/route/v1/driving/{Coordenadas(pontosEmOrdem)}?overview=full&geometries=polyline";
                using JsonDocument json = Consultar(url);

                if (json.RootElement.GetProperty("code").GetString() != "Ok")
                {
                    return null;
                }

                return json.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetString();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Falha ao buscar o trajeto no OSRM.");
                return null;
            }
        }

        private JsonDocument Consultar(string url)
        {
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

                using var leitor = new StreamReader(resposta.Content.ReadAsStream());
                return JsonDocument.Parse(leitor.ReadToEnd());
            }
        }

        // OSRM usa "longitude,latitude" separados por ";"
        private static string Coordenadas(List<PontoGeografico> pontos)
        {
            return string.Join(";", pontos.Select(p =>
                p.Longitude.ToString(CultureInfo.InvariantCulture) + "," + p.Latitude.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
