using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Quem reage aos eventos da outbox (e-mail ao cliente, integrações...).
    // Hoje é chamado dentro da própria API; com Kafka, cada consumidor vira um assinante do tópico.
    public interface IConsumidorEvento
    {
        // Nome fixo e único: vai para mensagens_processadas (idempotência)
        string Nome { get; }

        bool Aceita(string topico, string tipoEvento);

        // Erro = a mensagem fica pendente e é tentada de novo no próximo ciclo
        void Processar(Outbox mensagem);
    }
}
