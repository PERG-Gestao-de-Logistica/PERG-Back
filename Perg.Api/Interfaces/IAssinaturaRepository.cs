using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IAssinaturaRepository
    {
        // Planos
        List<Plano> ListarPlanosAtivos();
        Plano? ObterPlano(Guid id);
        Plano? ObterPlanoGratis();
        void GarantirPlanos(IEnumerable<Plano> padrao);

        // Assinatura vigente (não cancelada) da empresa, com o plano
        Assinatura? ObterVigente(Guid empresaId);
        Assinatura? ObterPorIdNoGateway(string idNoGateway);

        // Cancela a atual (se houver) e grava a nova numa transação (índice: uma vigente por empresa)
        void Trocar(Assinatura? atual, Assinatura nova);

        int ContarPedidosDesde(Guid empresaId, DateTime desdeUtc);
        List<Guid> ListarUsuariosParaCobranca(Guid empresaId);
        void AdicionarNotificacao(Notificacao notificacao);

        void Salvar();
    }
}
