using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IOutboxRepository
    {
        // Mais antigas primeiro (mantém a ordem dos eventos de cada pedido/rota)
        List<Outbox> ListarPendentes(int quantidade);

        bool JaProcessada(string consumidor, Guid mensagemId);
        void RegistrarProcessada(string consumidor, Guid mensagemId);

        void Salvar();
    }
}
