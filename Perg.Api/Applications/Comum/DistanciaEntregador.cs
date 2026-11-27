using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;

namespace Perg.Api.Applications.Comum
{
    // Regra única de "perto da loja", usada nas ofertas de rota e na tela Entregadores
    public static class DistanciaEntregador
    {
        // Raio a partir do ponto de coleta. Favoritos têm raio maior: a loja escolheu essas pessoas.
        public static double RaioRedeKm(IConfiguration config) => config.GetValue("Ofertas:RaioRedeKm", 10.0);
        public static double RaioFavoritosKm(IConfiguration config) => config.GetValue("Ofertas:RaioFavoritosKm", 20.0);

        // Linha reta (km) da última posição do entregador até a origem; null sem posição recente ou sem coordenadas
        public static double? KmAte(PosicaoAtual? posicao, Origem? origem)
        {
            if (posicao == null || origem?.Latitude == null || origem.Longitude == null) return null;

            const double RaioTerraKm = 6371;
            static double ParaRad(double graus) => graus * Math.PI / 180;

            double lat1 = ParaRad((double)posicao.Latitude);
            double lat2 = ParaRad((double)origem.Latitude.Value);
            double dLat = lat2 - lat1;
            double dLng = ParaRad((double)origem.Longitude.Value - (double)posicao.Longitude);
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLng / 2), 2);
            return 2 * RaioTerraKm * Math.Asin(Math.Sqrt(a));
        }

        // Favorito sem posição recente ainda conta (o GPS pode estar atrasado); rede precisa estar no raio
        public static bool NoRaio(double? km, bool favorito, IConfiguration config)
        {
            if (km == null) return favorito;
            return km <= (favorito ? RaioFavoritosKm(config) : RaioRedeKm(config));
        }
    }
}
