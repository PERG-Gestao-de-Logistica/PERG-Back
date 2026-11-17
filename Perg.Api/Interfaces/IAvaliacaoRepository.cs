using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IAvaliacaoRepository
    {
        Rota? ObterRotaComParadas(Guid rotaId);
        bool RotaJaAvaliada(Guid rotaId);
        void Adicionar(Avaliacao avaliacao);

        List<Rota> ListarRotasSemAvaliacao(DateTime desdeUtc, int quantidade);
        List<Avaliacao> ListarDaEmpresa(int quantidade);

        List<Avaliacao> ListarDoEntregador(Guid entregadorId);
    }
}
