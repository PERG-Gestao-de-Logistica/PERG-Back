using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Geocodificacao
{
    public class ResultadoGeocodificacao
    {
        // Ok = achou com precisão; Revisar = achou, mas aproximado; Falhou = não achou;
        // Pendente = não deu para consultar agora (sem chave, sem internet, cota estourada)
        public StatusGeocodificacao Status { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Confianca { get; set; }

        public static ResultadoGeocodificacao Pendente()
        {
            return new ResultadoGeocodificacao { Status = StatusGeocodificacao.Pendente };
        }
    }
}
