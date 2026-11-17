using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public record AssinaturaNoGateway(string Gateway, string? IdNoGateway, string? LinkPagamento, DateOnly? ProximoVencimento);

    public interface ICobrancaAssinatura
    {
        // Cria a assinatura recorrente; a primeira cobrança vence hoje
        AssinaturaNoGateway Criar(Empresa empresa, Plano plano, Guid assinaturaId);

        void Cancelar(string idNoGateway);

        string? ObterLinkPendente(string idNoGateway);

        // Só para testes no sandbox: dá baixa na fatura em aberto como "recebida"
        bool MarcarPendenteComoPaga(string idNoGateway);
    }
}
