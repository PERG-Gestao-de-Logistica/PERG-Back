using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public record AssinaturaNoGateway(string Gateway, string? IdNoGateway, string? LinkPagamento, DateOnly? ProximoVencimento);

    // Cobrança mensal da loja. Hoje: Asaas (sandbox) ou simulada (sem Asaas:ApiKey).
    public interface ICobrancaAssinatura
    {
        // Cria a assinatura recorrente; a primeira cobrança vence hoje
        AssinaturaNoGateway Criar(Empresa empresa, Plano plano, Guid assinaturaId);

        void Cancelar(string idNoGateway);

        // Link da fatura em aberto (boleto/Pix/cartão), se houver
        string? ObterLinkPendente(string idNoGateway);

        // Só para testes no sandbox: dá baixa na fatura em aberto como "recebida"
        bool MarcarPendenteComoPaga(string idNoGateway);
    }
}
