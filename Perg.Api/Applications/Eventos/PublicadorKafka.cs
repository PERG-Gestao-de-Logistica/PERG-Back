using System.Text;
using Confluent.Kafka;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Eventos
{
    // Envia o evento da outbox para o Kafka: tópico perg.<outbox.topico>, chave = id do pedido/rota
    // (mesma chave = mesma partição = ordem garantida), valor = payload JSON, id/tipo/data nos cabeçalhos.
    // Se o Kafka não confirmar, lança exceção: a mensagem continua pendente e o ProcessadorOutbox tenta de novo.
    public class PublicadorKafka : IPublicadorEventos
    {
        private readonly IProducer<string, string> _produtor;
        private readonly IConfiguration _configuracao;

        public PublicadorKafka(IProducer<string, string> produtor, IConfiguration configuracao)
        {
            _produtor = produtor;
            _configuracao = configuracao;
        }

        public void Publicar(Outbox mensagem)
        {
            var kafka = new Message<string, string>
            {
                Key = mensagem.Chave,
                Value = mensagem.Payload,
                Headers = new Headers
                {
                    { CabecalhosKafka.Id, Encoding.UTF8.GetBytes(mensagem.Id.ToString()) },
                    { CabecalhosKafka.TipoEvento, Encoding.UTF8.GetBytes(mensagem.TipoEvento) },
                    { CabecalhosKafka.CriadoEm, Encoding.UTF8.GetBytes(mensagem.CriadoEm.ToString("O")) }
                }
            };

            // Espera a confirmação (código síncrono, como o resto da API)
            _produtor.ProduceAsync(ConfiguracaoKafka.NomeTopico(_configuracao, mensagem.Topico), kafka).GetAwaiter().GetResult();
        }
    }

    public static class CabecalhosKafka
    {
        public const string Id = "id";
        public const string TipoEvento = "tipo_evento";
        public const string CriadoEm = "criado_em";
    }
}
