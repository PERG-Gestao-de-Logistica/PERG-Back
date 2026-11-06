using Perg.Api.Applications.Pagamentos;

namespace Perg.Api.Interfaces
{
    public enum SituacaoTransferencia { Processando, Concluida, Falhou }

    public record ResultadoTransferencia(SituacaoTransferencia Situacao, string? IdNoGateway, string? Erro);

    // Envio de Pix para o entregador. Hoje: Asaas (ou simulado, sem chave configurada).
    public interface IGatewayPagamento
    {
        ResultadoTransferencia TransferirPix(Guid saqueId, decimal valor, ChavePixNormalizada chave, string descricao);

        // Cancela uma transferência ainda pendente (usado pela simulação do sandbox para não acumular pendências)
        void CancelarTransferencia(string idNoGateway);
    }
}
