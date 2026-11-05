using Perg.Api.Applications.Roteamento;

namespace Perg.Api.Interfaces
{
    public interface IMatrizDistancias
    {
        MatrizDistancias Calcular(List<PontoGeografico> pontos);

        // Desenho da rota pelas ruas (polyline), para o mapa. Null se não conseguir.
        string? Trajeto(List<PontoGeografico> pontosEmOrdem);
    }
}
