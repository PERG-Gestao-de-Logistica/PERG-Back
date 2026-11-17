using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IPosicaoRepository
    {
        Entregador? ObterEntregador(Guid entregadorId);
        bool TemJornadaAberta(Guid entregadorId);

        Rota? ObterRotaEmAndamento(Guid entregadorId);

        // INSERT ... ON CONFLICT DO NOTHING: posição reenviada pelo app é ignorada
        void Inserir(List<PosicaoEntregador> posicoes);

        List<PosicaoEntregador> ListarTrilha(Guid rotaId);
    }
}
