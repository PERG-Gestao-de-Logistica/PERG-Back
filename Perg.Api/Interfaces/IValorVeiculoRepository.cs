using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IValorVeiculoRepository
    {
        // Só os valores personalizados da empresa logada (filtro global por empresa)
        List<ValorVeiculoEmpresa> ListarDaEmpresa();

        // Troca todos os valores personalizados da empresa pelos novos (lista vazia = volta para o padrão)
        void Substituir(Guid empresaId, List<ValorVeiculoEmpresa> novos);
    }
}
