using Perg.Api.Applications.Services;

namespace Perg.Api.Applications.Pagamentos
{
    // Só roda com o Asaas sandbox (ver Program.cs): conclui os saques "processando" depois de
    // Asaas:SegundosParaConcluir, simulando o webhook TRANSFER_DONE que o sandbox nunca manda.
    public class ConclusaoSaquesSandbox : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConclusaoSaquesSandbox> _logger;
        private readonly TimeSpan _espera;

        public ConclusaoSaquesSandbox(IServiceScopeFactory scopeFactory, ILogger<ConclusaoSaquesSandbox> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _espera = TimeSpan.FromSeconds(config.GetValue("Asaas:SegundosParaConcluir", 20));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("Simulação do Asaas sandbox ligada: saques viram \"pago\" após {Segundos}s.", _espera.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope escopo = _scopeFactory.CreateScope();
                    escopo.ServiceProvider.GetRequiredService<SaqueService>().ConcluirSaquesDoSandbox(_espera);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha na simulação de conclusão de saques.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }
    }
}
