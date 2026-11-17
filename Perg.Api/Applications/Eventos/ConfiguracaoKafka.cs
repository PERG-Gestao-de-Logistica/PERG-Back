using Confluent.Kafka;

namespace Perg.Api.Applications.Eventos
{
    // Lê a seção "Kafka" dos Segredos do Usuário. Sem Kafka:Servidores, a API usa o PublicadorInterno.
    //   Servidores        host:porta do cluster (Aiven: "Service URI")
    //   Usuario / Senha   SASL SCRAM-SHA-256 (Aiven: usuário avnadmin, com SASL ligado no serviço)
    //   CaCertificado     caminho do ca.pem baixado do Aiven (fora do repositório)
    //   CertificadoCliente / ChaveCliente   alternativa ao SASL: service.cert e service.key do Aiven
    //   PrefixoTopicos    padrão "perg." → tópicos perg.pedido, perg.rota, perg.roteirizacao
    //   GrupoConsumidor   padrão "perg-api"
    public static class ConfiguracaoKafka
    {
        // Mesmos valores do CHECK da coluna outbox.topico
        public static readonly string[] TopicosOutbox = { "pedido", "rota", "roteirizacao" };

        public static bool Ligado(IConfiguration configuracao) => !string.IsNullOrWhiteSpace(configuracao["Kafka:Servidores"]);

        public static string Prefixo(IConfiguration configuracao) => configuracao["Kafka:PrefixoTopicos"] ?? "perg.";

        public static string NomeTopico(IConfiguration configuracao, string topicoOutbox) => Prefixo(configuracao) + topicoOutbox;

        public static List<string> NomesTopicos(IConfiguration configuracao) => TopicosOutbox.Select(t => NomeTopico(configuracao, t)).ToList();

        public static ClientConfig Cliente(IConfiguration configuracao)
        {
            var config = new ClientConfig
            {
                BootstrapServers = configuracao["Kafka:Servidores"],
                ClientId = "perg-api"
            };

            string? ca = configuracao["Kafka:CaCertificado"];
            if (!string.IsNullOrWhiteSpace(ca) && !File.Exists(ca))
            {
                // Sem isso o erro da biblioteca é "ssl.ca.location failed: BIO lib", que não diz o que houve
                throw new InvalidOperationException($"Kafka: arquivo do certificado não encontrado em \"{ca}\". Confira o caminho em Kafka:CaCertificado.");
            }

            if (!string.IsNullOrWhiteSpace(configuracao["Kafka:Usuario"]))
            {
                config.SecurityProtocol = SecurityProtocol.SaslSsl;
                config.SaslMechanism = SaslMechanism.ScramSha256;
                config.SaslUsername = configuracao["Kafka:Usuario"];
                config.SaslPassword = configuracao["Kafka:Senha"];
                if (!string.IsNullOrWhiteSpace(ca)) config.SslCaLocation = ca;
            }
            else if (!string.IsNullOrWhiteSpace(configuracao["Kafka:CertificadoCliente"]))
            {
                config.SecurityProtocol = SecurityProtocol.Ssl;
                config.SslCertificateLocation = configuracao["Kafka:CertificadoCliente"];
                config.SslKeyLocation = configuracao["Kafka:ChaveCliente"];
                if (!string.IsNullOrWhiteSpace(ca)) config.SslCaLocation = ca;
            }

            return config;
        }

        public static ProducerConfig Produtor(IConfiguration configuracao)
        {
            return new ProducerConfig(Cliente(configuracao))
            {
                Acks = Acks.All,              // só confirma depois de gravado nas réplicas
                EnableIdempotence = true,     // reenvio do produtor não duplica nem troca a ordem
                MessageTimeoutMs = 10000      // sem resposta em 10 s = erro; a outbox tenta de novo
            };
        }

        public static ConsumerConfig Consumidor(IConfiguration configuracao)
        {
            return new ConsumerConfig(Cliente(configuracao))
            {
                GroupId = configuracao["Kafka:GrupoConsumidor"] ?? "perg-api",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false      // só confirma a leitura depois que todos os consumidores processaram
            };
        }
    }
}
