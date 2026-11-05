using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PedidosDto
{
    // Detalhe do pedido, com a linha do tempo
    public class LerPedidoDto
    {
        public Guid Id { get; set; }
        public int NumeroPedido { get; set; }
        public string? NumeroExterno { get; set; }
        public CanalPedido Canal { get; set; }
        public ModalidadePedido Modalidade { get; set; }
        public StatusPedido Status { get; set; }

        public Guid OrigemId { get; set; }
        public string OrigemNome { get; set; } = string.Empty;

        public string ClienteNome { get; set; } = string.Empty;
        public string? ClienteTelefone { get; set; }
        public string? ClienteEmail { get; set; }

        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Referencia { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public StatusGeocodificacao GeocodificacaoStatus { get; set; }

        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
        public decimal PesoKg { get; set; }
        public int Volumes { get; set; }
        public string? Observacoes { get; set; }

        // Link público que vai para o cliente final
        public string TokenRastreamento { get; set; } = string.Empty;

        public DateTime? EntregueEm { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }

        public List<LerPedidoEventoDto> Eventos { get; set; } = new();
    }

    public class LerPedidoEventoDto
    {
        public string TipoEvento { get; set; } = string.Empty;
        public StatusPedido? StatusNovo { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public AutorEvento AutorTipo { get; set; }
        public DateTime OcorridoEm { get; set; }
    }
}
