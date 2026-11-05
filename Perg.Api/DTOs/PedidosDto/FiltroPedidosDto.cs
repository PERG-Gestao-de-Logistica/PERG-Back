using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PedidosDto
{
    // Vem pela query string: /api/Pedidos?status=Novo&dataInicio=2026-11-01&busca=maria&pagina=1
    public class FiltroPedidosDto
    {
        public StatusPedido? Status { get; set; }

        // Datas no fuso da loja (data de criação do pedido)
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        // Número do pedido, número externo ou nome do cliente
        public string? Busca { get; set; }

        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }
}
