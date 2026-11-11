using Perg.Api.Applications.TempoReal;

namespace Perg.Api.Interfaces
{
    // Última posição de cada entregador (vale por 10 minutos).
    // Sem ConnectionStrings:Redis: memória do servidor. Com Redis: compartilhada entre servidores.
    public interface IPosicaoAtualCache
    {
        void Atualizar(PosicaoAtual posicao);
        PosicaoAtual? Obter(Guid entregadorId);
        List<PosicaoAtual> ListarDaEmpresa(Guid empresaId);
    }
}
