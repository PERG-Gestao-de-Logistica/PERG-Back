using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PedidosDto
{
    // Linha da listagem de pedidos
    public class LerPedidoResumoDto
    {
        public Guid Id { get; set; }
        public int NumeroPedido { get; set; }
        public string? NumeroExterno { get; set; }
        public StatusPedido Status { get; set; }
        public ModalidadePedido Modalidade { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
        public int Volumes { get; set; }
        public StatusGeocodificacao GeocodificacaoStatus { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
