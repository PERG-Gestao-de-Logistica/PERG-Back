using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Pagamentos
{
    // Usado quando Asaas:ApiKey não está configurada: aprova na hora, sem mexer em dinheiro de verdade
    public class GatewaySimulado : IGatewayPagamento
    {
        private readonly ILogger<GatewaySimulado> _logger;

        public GatewaySimulado(ILogger<GatewaySimulado> logger)
        {
            _logger = logger;
        }

        public ResultadoTransferencia TransferirPix(Guid saqueId, decimal valor, ChavePixNormalizada chave, string descricao)
        {
            _logger.LogWarning("Saque {Saque} de R$ {Valor} SIMULADO (Asaas:ApiKey não configurada).", saqueId, valor);
            return new ResultadoTransferencia(SituacaoTransferencia.Concluida, "simulado-" + saqueId.ToString("N")[..12], null);
        }

        public void CancelarTransferencia(string idNoGateway)
        {
            // nada a cancelar: o simulado já conclui na hora
        }
    }
}
