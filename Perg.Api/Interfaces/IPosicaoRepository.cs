using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IPosicaoRepository
    {
        Entregador? ObterEntregador(Guid entregadorId);
        bool TemJornadaAberta(Guid entregadorId);

        // Rota em andamento do entregador, com as paradas (para saber qual cliente é o próximo)
        Rota? ObterRotaEmAndamento(Guid entregadorId);

        // INSERT ... ON CONFLICT DO NOTHING: posição reenviada pelo app é ignorada
        void Inserir(List<PosicaoEntregador> posicoes);

        // Percurso real de uma rota da loja logada
        List<PosicaoEntregador> ListarTrilha(Guid rotaId);
    }
}
