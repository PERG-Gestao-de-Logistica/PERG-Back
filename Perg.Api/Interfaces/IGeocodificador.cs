using Perg.Api.Applications.Geocodificacao;

namespace Perg.Api.Interfaces
{
    public interface IGeocodificador
    {
        ResultadoGeocodificacao Geocodificar(EnderecoGeocodificacao endereco);
    }
}
