using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface INotificacaoRepository
    {
        (List<Notificacao> Itens, int Total) Listar(Guid usuarioId, bool somenteNaoLidas, int pagina, int tamanhoPagina);
        int ContarNaoLidas(Guid usuarioId);
        Notificacao? ObterPorId(Guid id, Guid usuarioId);
        int MarcarTodasComoLidas(Guid usuarioId, DateTime lidaEm);
        Usuario? ObterUsuario(Guid usuarioId);
        void Salvar();
    }
}
