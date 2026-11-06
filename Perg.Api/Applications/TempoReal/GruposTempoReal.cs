namespace Perg.Api.Applications.TempoReal
{
    // Nomes dos grupos do SignalR (quem recebe cada mensagem)
    public static class GruposTempoReal
    {
        // Todos os usuários logados de uma loja: posições das rotas em andamento
        public static string Empresa(Guid empresaId) => $"empresa:{empresaId}";

        // Um usuário: notificações do sino
        public static string Usuario(Guid usuarioId) => $"usuario:{usuarioId}";

        // Quem está com a página de rastreio de um pedido aberta
        public static string Pedido(Guid pedidoId) => $"pedido:{pedidoId}";
    }
}
