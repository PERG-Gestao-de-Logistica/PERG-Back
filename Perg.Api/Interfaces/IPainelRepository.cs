namespace Perg.Api.Interfaces
{
    // Contagens da visão geral (o filtro global já limita à empresa do usuário)
    public interface IPainelRepository
    {
        int ContarPedidosDesde(DateTime desdeUtc);
        bool ExistePedido();
        bool ExisteOrigem();
        bool ExisteFavorito();
    }
}
