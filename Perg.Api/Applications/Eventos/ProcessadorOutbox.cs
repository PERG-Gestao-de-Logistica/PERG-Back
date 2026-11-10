using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Eventos
{
    // Publica os eventos gravados na outbox (padrão "transactional outbox"):
    // o evento entra no banco junto com a mudança do pedido e é enviado depois, sem risco de se perder.
    // Se uma mensagem falhar, as seguintes da mesma chave (mesmo pedido/rota) esperam, para não sair de ordem.
    public class ProcessadorOutbox : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(3);
        private const int Lote = 50;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessadorOutbox> _logger;

        public ProcessadorOutbox(IServiceScopeFactory scopeFactory, ILogger<ProcessadorOutbox> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int publicadas = 0;
                try
                {
                    publicadas = ProcessarLote(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao ler a outbox.");
                }

                // Lote cheio: provavelmente tem mais na fila, segue sem esperar
                if (publicadas < Lote)
                {
                    await Task.Delay(Intervalo, stoppingToken);
                }
            }
        }

        private int ProcessarLote(CancellationToken parar)
        {
            using IServiceScope escopo = _scopeFactory.CreateScope();
            var repository = escopo.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publicador = escopo.ServiceProvider.GetRequiredService<IPublicadorEventos>();

            List<Outbox> pendentes = repository.ListarPendentes(Lote);
            var chavesTravadas = new HashSet<string>();
            int publicadas = 0;

            foreach (Outbox mensagem in pendentes)
            {
                if (parar.IsCancellationRequested) break;
                if (chavesTravadas.Contains(mensagem.Chave)) continue;

                try
                {
                    publicador.Publicar(mensagem);
                    mensagem.PublicadoEm = DateTime.UtcNow;
                    repository.Salvar();
                    publicadas++;
                }
                catch (Exception ex)
                {
                    chavesTravadas.Add(mensagem.Chave);
                    _logger.LogWarning(ex, "Evento {Tipo} ({Id}) não publicado; tenta de novo no próximo ciclo.", mensagem.TipoEvento, mensagem.Id);
                }
            }

            return publicadas;
        }
    }
}
