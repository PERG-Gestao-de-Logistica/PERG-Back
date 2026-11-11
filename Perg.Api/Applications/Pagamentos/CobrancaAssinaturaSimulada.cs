using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Pagamentos
{
    // Sem Asaas:ApiKey: a assinatura nasce paga, sem link de pagamento
    public class CobrancaAssinaturaSimulada : ICobrancaAssinatura
    {
        public AssinaturaNoGateway Criar(Empresa empresa, Plano plano, Guid assinaturaId)
        {
            return new AssinaturaNoGateway("simulado", "simulado-" + assinaturaId.ToString("N")[..12], null,
                DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1));
        }

        public void Cancelar(string idNoGateway) { }

        public string? ObterLinkPendente(string idNoGateway) => null;

        public bool MarcarPendenteComoPaga(string idNoGateway) => true;
    }
}
