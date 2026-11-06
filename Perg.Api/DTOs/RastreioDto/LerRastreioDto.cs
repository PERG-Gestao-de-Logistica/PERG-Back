using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.RastreioDto
{
    // O que o cliente final vê pelo link. Sem dados internos da loja.
    public class LerRastreioDto
    {
        public int NumeroPedido { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public StatusPedido Status { get; set; }
        public ModalidadePedido Modalidade { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
        public DateTime? EntregueEm { get; set; }

        // O cliente informa este código ao entregador para confirmar o recebimento
        public string? PinEntrega { get; set; }

        // Com o pedido em rota: quantas entregas faltam antes da sua
        public int? EntregasAntes { get; set; }

        // Só quando a sua é a próxima entrega; as atualizações chegam pelo SignalR
        public PosicaoEntregadorRastreioDto? PosicaoEntregador { get; set; }
        public List<LerRastreioEventoDto> Eventos { get; set; } = new();
    }

    public class LerRastreioEventoDto
    {
        public string Descricao { get; set; } = string.Empty;
        public StatusPedido? StatusNovo { get; set; }
        public DateTime OcorridoEm { get; set; }
    }

    public class PosicaoEntregadorRastreioDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime AtualizadaEm { get; set; }
    }
}
