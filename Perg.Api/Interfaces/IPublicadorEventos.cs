using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Para onde vão os eventos da outbox. Hoje: PublicadorInterno (entrega direto aos consumidores da API).
    // No deploy: um publicador Kafka com a mesma interface (tópico = outbox.topico, chave = outbox.chave).
    public interface IPublicadorEventos
    {
        void Publicar(Outbox mensagem);
    }
}
