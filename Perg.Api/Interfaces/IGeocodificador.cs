using Perg.Api.Applications.Geocodificacao;

namespace Perg.Api.Interfaces
{
    // Transforma endereço em latitude/longitude. Hoje: Google. Dá para trocar sem mexer nos services.
    public interface IGeocodificador
    {
        ResultadoGeocodificacao Geocodificar(EnderecoGeocodificacao endereco);
    }
}
