using Perg.Api.Applications.TempoReal;

namespace Perg.Api.Interfaces
{
    // Última posição de cada entregador (vale por 10 minutos).
    public interface IPosicaoAtualCache
    {
        void Atualizar(PosicaoAtual posicao);
        PosicaoAtual? Obter(Guid entregadorId);
        List<PosicaoAtual> ListarDaEmpresa(Guid empresaId);
    }
}
