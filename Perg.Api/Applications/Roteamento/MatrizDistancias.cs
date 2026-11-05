namespace Perg.Api.Applications.Roteamento
{
    // Distância (metros) e tempo de viagem (segundos) entre cada par de pontos.
    // Índice 0 = origem (loja); 1..n = pedidos.
    public class MatrizDistancias
    {
        public long[,] Metros { get; set; } = new long[0, 0];
        public long[,] Segundos { get; set; } = new long[0, 0];

        // true = calculada em linha reta (o OSRM não respondeu); os números são aproximados
        public bool Estimada { get; set; }
    }

    public class PontoGeografico
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public PontoGeografico(decimal latitude, decimal longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
