using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IOfertaRepository
    {
        Rota? ObterRota(Guid rotaId);
        Empresa? ObterEmpresa(Guid empresaId);

        // Ativos, com o veículo do tipo, expediente aberto e sem rota aceita/em andamento em nenhuma loja.
        // Com favoritosDaEmpresa: só os favoritos dessa loja.
        List<Guid> ListarLivresEmExpediente(TipoVeiculo tipo, Guid? favoritosDaEmpresa, List<Guid> excluir);
        bool EstaComRota(Guid entregadorId);
        bool TemJornadaAberta(Guid entregadorId);

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
