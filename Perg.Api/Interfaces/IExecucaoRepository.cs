using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Usado pelo app do entregador
    public interface IExecucaoRepository
    {
        Entregador? ObterEntregador(Guid entregadorId);

        Jornada? ObterJornadaAberta(Guid entregadorId);
        void AdicionarJornada(Jornada jornada);
        bool TemRotaEmAndamento(Guid entregadorId);

        List<Rota> ListarRotasDoEntregador(Guid entregadorId);
        Rota? ObterRotaDoEntregador(Guid rotaId, Guid entregadorId);
        Parada? ObterParadaDoEntregador(Guid paradaId, Guid entregadorId);

        List<Guid> ListarUsuariosParaNotificar(Guid empresaId, bool ocorrencia);

        decimal ObterSaldo(Guid entregadorId);
        List<CarteiraLancamento> ListarLancamentos(Guid entregadorId, int quantidade);

        void Adicionar(object entidade);
        void Salvar();
    }
}
