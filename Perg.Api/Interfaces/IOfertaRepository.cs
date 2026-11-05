using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IOfertaRepository
    {
        Rota? ObterRota(Guid rotaId);
        Empresa? ObterEmpresa(Guid empresaId);

        // Candidatos: entregadores ativos com veículo atual do tipo da rota
        List<Guid> ListarFavoritosDisponiveis(Guid empresaId, TipoVeiculo tipo);
        List<Guid> ListarRedeDisponivel(TipoVeiculo tipo, List<Guid> excluir, int limite);

        List<OfertaRota> ListarAbertasDoEntregador(Guid entregadorId, DateTime agora);
        OfertaRota? ObterParaResposta(Guid ofertaId, Guid entregadorId);
        List<OfertaRota> ListarDaRota(Guid rotaId);

        // Processamento em segundo plano (sem filtro de empresa)
        List<OfertaRota> ListarVencidas(DateTime agora);
        List<Rota> ListarOfertadasSemOfertaAberta();
        List<Guid> ListarAdminsDaEmpresa(Guid empresaId);

        void Adicionar(IEnumerable<OfertaRota> ofertas);
        void Adicionar(IEnumerable<PedidoEvento> eventos, IEnumerable<Outbox> mensagens, IEnumerable<Notificacao> notificacoes);
        void Salvar();
    }
}
