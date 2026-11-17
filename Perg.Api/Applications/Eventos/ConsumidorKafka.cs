using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Perg.Api.Domains;

namespace Perg.Api.Applications.Eventos
{
    // Lê os tópicos perg.* e entrega cada evento aos consumidores da API (e-mail ao cliente, webhooks...),
    // usando o PublicadorInterno, que já garante "uma vez por consumidor" via mensagens_processadas.
    // A leitura só é confirmada (commit) depois que todos processaram; se algum falhar, espera 5 s e lê de novo
    // a mesma mensagem, sem pular para a próxima (mantém a ordem do pedido/rota).
    public class ConsumidorKafka : BackgroundService
    {
        private static readonly TimeSpan EsperaAposErro = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuracao;
        private readonly ILogger<ConsumidorKafka> _logger;

        public ConsumidorKafka(IServiceScopeFactory scopeFactory, IConfiguration configuracao, ILogger<ConsumidorKafka> logger)
        {
            _scopeFactory = scopeFactory;
            _configuracao = configuracao;
            _logger = logger;
        }

        // Consume() bloqueia a thread: roda numa thread própria para não segurar a subida da API
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(() => Executar(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Executar(CancellationToken parar)
        {
            IConsumer<string, string> criado;
            try
            {
                criado = new ConsumerBuilder<string, string>(ConfiguracaoKafka.Consumidor(_configuracao))
                    .SetErrorHandler((_, erro) => _logger.LogWarning("Kafka (consumidor): {Erro}", erro.Reason))
                    .Build();
            }
            catch (Exception ex) when (ex is InvalidOperationException or KafkaException or ArgumentException)
            {
                // Configuração errada não derruba a API: só avisa no Output (os eventos ficam no Kafka esperando)
                _logger.LogError("Consumidor do Kafka não iniciou: {Erro}", ex.Message);
                return;
            }

            using IConsumer<string, string> consumidor = criado;

            consumidor.Subscribe(ConfiguracaoKafka.NomesTopicos(_configuracao));

            while (!parar.IsCancellationRequested)
            {
                ConsumeResult<string, string>? resultado;
                try
                {
                    resultado = consumidor.Consume(parar);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning("Kafka: erro ao ler ({Erro}). Confira se os tópicos {Topicos} existem.",
                        ex.Error.Reason, string.Join(", ", ConfiguracaoKafka.NomesTopicos(_configuracao)));
                    parar.WaitHandle.WaitOne(EsperaAposErro);
                    continue;
                }

                if (resultado?.Message == null) continue;

                try
                {
                    Processar(resultado);
                    consumidor.Commit(resultado);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Evento {Offset} não processado; tenta de novo em {Segundos} s.",
                        resultado.TopicPartitionOffset, EsperaAposErro.TotalSeconds);
                    consumidor.Seek(resultado.TopicPartitionOffset);
                    parar.WaitHandle.WaitOne(EsperaAposErro);
                }
            }

            consumidor.Close(); // sai do grupo na hora (sem esperar o timeout da sessão)
        }

        private void Processar(ConsumeResult<string, string> resultado)
        {
            string? id = Cabecalho(resultado, CabecalhosKafka.Id);
            if (id == null || !Guid.TryParse(id, out Guid mensagemId))
            {
                // Mensagem que não veio da nossa outbox: ignora (e confirma, para não travar a partição)
                _logger.LogWarning("Mensagem sem cabeçalho \"id\" em {Offset}; ignorada.", resultado.TopicPartitionOffset);
                return;
            }

            string prefixo = ConfiguracaoKafka.Prefixo(_configuracao);
            string criadoEm = Cabecalho(resultado, CabecalhosKafka.CriadoEm) ?? "";

            var mensagem = new Outbox
            {
                Id = mensagemId,
                Topico = resultado.Topic.StartsWith(prefixo) ? resultado.Topic[prefixo.Length..] : resultado.Topic,
                Chave = resultado.Message.Key,
                TipoEvento = Cabecalho(resultado, CabecalhosKafka.TipoEvento) ?? "",
                Payload = resultado.Message.Value,
                CriadoEm = DateTime.TryParse(criadoEm, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime data)
                    ? data
                    : DateTime.UtcNow
            };

            using IServiceScope escopo = _scopeFactory.CreateScope();
            escopo.ServiceProvider.GetRequiredService<PublicadorInterno>().Publicar(mensagem);
        }

        private static string? Cabecalho(ConsumeResult<string, string> resultado, string nome)
        {
            return resultado.Message.Headers != null && resultado.Message.Headers.TryGetLastBytes(nome, out byte[] valor)
                ? Encoding.UTF8.GetString(valor)
                : null;
        }
    }
}
