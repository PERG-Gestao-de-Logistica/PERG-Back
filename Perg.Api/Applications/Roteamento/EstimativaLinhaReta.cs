namespace Perg.Api.Applications.Roteamento
{
    // Plano B quando o OSRM não responde: distância em linha reta × 1,3 (ruas não são retas)
    // e velocidade média urbana de 25 km/h.
    public static class EstimativaLinhaReta
    {
        private const double FatorRuas = 1.3;
        private const double VelocidadeMetrosPorSegundo = 25_000.0 / 3600.0;

        public static MatrizDistancias Calcular(List<PontoGeografico> pontos)
        {
            int n = pontos.Count;
            var matriz = new MatrizDistancias { Metros = new long[n, n], Segundos = new long[n, n], Estimada = true };

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double metros = i == j ? 0 : Haversine(pontos[i], pontos[j]) * FatorRuas;
                    matriz.Metros[i, j] = (long)Math.Round(metros);
                    matriz.Segundos[i, j] = (long)Math.Round(metros / VelocidadeMetrosPorSegundo);
                }
            }

            return matriz;
        }

        private static double Haversine(PontoGeografico a, PontoGeografico b)
        {
            const double RaioTerraMetros = 6_371_000;
            double lat1 = (double)a.Latitude * Math.PI / 180;
            double lat2 = (double)b.Latitude * Math.PI / 180;
            double dLat = lat2 - lat1;
            double dLon = ((double)b.Longitude - (double)a.Longitude) * Math.PI / 180;

            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return 2 * RaioTerraMetros * Math.Asin(Math.Sqrt(h));
        }
    }
}
