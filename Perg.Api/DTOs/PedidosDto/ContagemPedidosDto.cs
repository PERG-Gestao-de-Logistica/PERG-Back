namespace Perg.Api.DTOs.PedidosDto
{
    // GET /api/Pedidos/contagem: números das abas da tela de pedidos
    public class ContagemPedidosDto
    {
        public int Todos { get; set; }
        public int Novos { get; set; }

        // Novos que entram na roteirização de hoje (localização ok, janela válida, não é retirada)
        public int Prontos { get; set; }

        // Novos que não podem ser roteirizados até alguém corrigir (endereço, janela vencida...)
        public int Problemas { get; set; }

        public int EmRota { get; set; }
        public int Entregues { get; set; }
        public int Ocorrencias { get; set; }
        public int Cancelados { get; set; }
    }
}
