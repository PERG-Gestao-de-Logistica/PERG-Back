namespace Perg.Api.DTOs.AssinaturasDto
{
    public class LerPlanoDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoMensal { get; set; }
        public int? LimitePedidosMes { get; set; }   // null = ilimitado
    }

    public class LerAssinaturaDto
    {
        public LerPlanoDto Plano { get; set; } = new();
        public string Status { get; set; } = string.Empty;          // Ativa, Inadimplente
        public DateOnly? ProximoVencimento { get; set; }
        public int PedidosNoMes { get; set; }
        public string? LinkPagamento { get; set; }                  // fatura em aberto (boleto/Pix/cartão)
    }

    public class TrocarPlanoDto
    {
        public Guid PlanoId { get; set; }
    }
}
