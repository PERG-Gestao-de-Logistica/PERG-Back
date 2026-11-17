using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Para onde vão os eventos da outbox: PublicadorKafka (tópico perg.<outbox.topico>, chave = outbox.chave)
    // quando Kafka:Servidores está configurado; senão PublicadorInterno, que entrega direto aos consumidores da API.
    public interface IPublicadorEventos
    {
        void Publicar(Outbox mensagem);
    }
}
