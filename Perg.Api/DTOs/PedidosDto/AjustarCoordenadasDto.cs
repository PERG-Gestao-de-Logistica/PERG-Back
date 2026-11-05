namespace Perg.Api.DTOs.PedidosDto
{
    // A loja arrasta o pino no mapa quando o endereço não foi achado ou ficou impreciso
    public class AjustarCoordenadasDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
