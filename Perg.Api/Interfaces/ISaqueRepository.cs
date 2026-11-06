using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface ISaqueRepository
    {
        Entregador? ObterEntregador(Guid entregadorId);

        // Trava o entregador (SELECT ... FOR UPDATE), confere o saldo e grava saque + débito juntos.
        // Devolve null se o saldo não cobre o valor.
        Saque? RegistrarSaque(Guid entregadorId, decimal valor, string chavePix);

        bool TemSaqueEmAndamento(Guid entregadorId);
        List<Saque> ListarDoEntregador(Guid entregadorId, int quantidade);

        // Saques ainda "processando" pedidos antes de um horário (simulação do sandbox)
        List<Saque> ListarProcessandoAntesDe(DateTime limite);

        Saque? ObterPorId(Guid saqueId);
        Saque? ObterPorIdNoGateway(string idNoGateway);

        // Devolve o valor para a carteira (estorno), uma única vez por saque
        void Estornar(Saque saque, string motivo);

        void Salvar();
    }
}
