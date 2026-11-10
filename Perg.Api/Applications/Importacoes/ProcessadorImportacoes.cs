using Perg.Api.Applications.Services;
using Perg.Api.Contexts;
using Perg.Api.DTOs.ImportacoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Applications.Importacoes
{
    // Processa as importações confirmadas, uma por vez, linha a linha.
    // Cada linha usa um escopo novo (contexto limpo): um erro numa linha não contamina as seguintes.
    // A geocodificação (Nominatim, 1 por segundo) é o que mais demora: ~1 s por pedido.
    public class ProcessadorImportacoes : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessadorImportacoes> _logger;

        public ProcessadorImportacoes(IServiceScopeFactory scopeFactory, ILogger<ProcessadorImportacoes> logger)
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
                    ProcessarProxima(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no processamento de importações.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }

        private void ProcessarProxima(CancellationToken parar)
        {
            PlanoImportacao? plano;
            using (IServiceScope escopo = _scopeFactory.CreateScope())
            {
                plano = escopo.ServiceProvider.GetRequiredService<ImportacaoService>().PrepararProxima();
            }
            if (plano == null) return;

            _logger.LogInformation("Importação {Id}: {Linhas} linha(s) para processar.", plano.ImportacaoId, plano.Linhas.Count);

            foreach (LinhaPlanilha linha in plano.Linhas)
            {
                if (parar.IsCancellationRequested) break; // continua de onde parou quando a API voltar

                using (IServiceScope escopo = _scopeFactory.CreateScope())
                {
                    // Sem usuário logado: liga o filtro na empresa da importação (origem, pedidos repetidos)
                    escopo.ServiceProvider.GetRequiredService<PergDbContext>().EmpresaIdAtual = plano.EmpresaId;

                    try
                    {
                        escopo.ServiceProvider.GetRequiredService<ImportacaoService>().ImportarLinha(plano, linha);
                        plano.Importadas++;
                    }
                    catch (DomainException ex)
                    {
                        plano.Erros.Add(new ErroImportacaoDto { Linha = linha.Numero, Motivo = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Importação {Id}, linha {Linha}.", plano.ImportacaoId, linha.Numero);
                        plano.Erros.Add(new ErroImportacaoDto { Linha = linha.Numero, Motivo = "Erro inesperado ao gravar o pedido (confira tamanhos dos campos)." });
                    }
                }

                // Progresso a cada linha: o front acompanha pelo GET e, se a API cair, retoma sem repetir pedidos
                SalvarProgresso(plano, concluir: false);
            }

            SalvarProgresso(plano, concluir: !parar.IsCancellationRequested);
        }

        private void SalvarProgresso(PlanoImportacao plano, bool concluir)
        {
            using IServiceScope escopo = _scopeFactory.CreateScope();
            escopo.ServiceProvider.GetRequiredService<ImportacaoService>().RegistrarProgresso(plano, concluir);
        }
    }
}
