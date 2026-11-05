using Perg.Api.Applications.Services;

namespace Perg.Api.Applications.Ofertas
{
    // Roda junto com a API: a cada 15 s expira ofertas vencidas, passa dos favoritos para a rede
    // e devolve para "proposta" as rotas que ninguém aceitou.
    public class ProcessadorOfertas : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessadorOfertas> _logger;

        public ProcessadorOfertas(IServiceScopeFactory scopeFactory, ILogger<ProcessadorOfertas> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Escopo novo a cada rodada: sem usuário logado, o contexto não aplica filtro de empresa
                    using IServiceScope escopo = _scopeFactory.CreateScope();
                    escopo.ServiceProvider.GetRequiredService<OfertaService>().ProcessarRodadas();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar as rodadas de ofertas.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }
    }
}
