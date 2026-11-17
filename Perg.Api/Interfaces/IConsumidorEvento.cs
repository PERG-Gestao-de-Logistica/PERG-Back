using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Chamado pelo PublicadorInterno ou, com Kafka, pelo ConsumidorKafka depois de ler o tópico.
    public interface IConsumidorEvento
    {
        // Nome fixo e único: vai para mensagens_processadas (idempotência)
        string Nome { get; }

        bool Aceita(string topico, string tipoEvento);

        // Erro = a mensagem fica pendente e é tentada de novo no próximo ciclo
        void Processar(Outbox mensagem);
    }
}
