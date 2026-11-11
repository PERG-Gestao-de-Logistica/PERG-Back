using Perg.Api.Domains;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Assinaturas
{
    // Regras do plano na hora de criar pedido (manual ou planilha):
    // pagamento em atraso bloqueia; passou do limite do mês, bloqueia. Sem assinatura = plano grátis.
    public class LimitesPlano
    {
        private readonly IAssinaturaRepository _repository;
        private readonly IEmpresaRepository _empresas;

        public LimitesPlano(IAssinaturaRepository repository, IEmpresaRepository empresas)
        {
            _repository = repository;
            _empresas = empresas;
        }

        public void ConferirNovoPedido(Guid empresaId)
        {
            Assinatura? assinatura = _repository.ObterVigente(empresaId);

            if (assinatura?.Status == StatusAssinatura.Inadimplente)
            {
                throw new DomainException("A assinatura do Perg está com pagamento em atraso. Regularize em Configurações > Plano para criar novos pedidos.");
            }

            Plano? plano = assinatura?.Plano ?? _repository.ObterPlanoGratis();
            if (plano?.LimitePedidosMes == null)
            {
                return; // ilimitado (ou nenhum plano cadastrado)
            }

            int usados = _repository.ContarPedidosDesde(empresaId, InicioDoMesUtc(empresaId));
            if (usados >= plano.LimitePedidosMes.Value)
            {
                throw new DomainException($"Limite do plano {plano.Nome} atingido ({plano.LimitePedidosMes} pedidos por mês). Troque de plano para continuar.");
            }
        }

        public int PedidosNoMes(Guid empresaId)
        {
            return _repository.ContarPedidosDesde(empresaId, InicioDoMesUtc(empresaId));
        }

        // Dia 1 do mês atual no fuso da loja, convertido para UTC (o banco guarda em UTC)
        private DateTime InicioDoMesUtc(Guid empresaId)
        {
            Empresa? empresa = _empresas.ObterPorId(empresaId);
            TimeZoneInfo fuso = TimeZoneInfo.FindSystemTimeZoneById(empresa?.FusoHorario ?? "America/Sao_Paulo");
            DateTime agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso);
            var inicioLocal = new DateTime(agoraLocal.Year, agoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(inicioLocal, fuso);
        }
    }
}
