using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.RotasDto
{
    public class LerRotaDto
    {
        public Guid Id { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public short Numero { get; set; }
        public string? Regiao { get; set; }
        public TipoVeiculo TipoVeiculo { get; set; }
        public StatusRota Status { get; set; }
        public string? Cor { get; set; }
        public DateOnly DataOperacao { get; set; }
        public int? DistanciaPrevistaM { get; set; }
        public int? DuracaoPrevistaS { get; set; }
        public DateTime? ColetaPrevistaEm { get; set; }
        public DateTime? FimPrevistoEm { get; set; }

        public decimal? ValorEntregador { get; set; }

        public Guid? EntregadorId { get; set; }
        public string? EntregadorNome { get; set; }

        // Para o monitoramento da loja (ligar para o entregador, conferir o veículo)
        public string? EntregadorTelefone { get; set; }
        public string? VeiculoPlaca { get; set; }
        public string? VeiculoModelo { get; set; }

        // Polyline do OSRM para desenhar no mapa (pode vir null)
        public string? Trajeto { get; set; }

        public List<LerParadaDto> Paradas { get; set; } = new();
    }

    public class LerParadaDto
    {
        public Guid Id { get; set; }
        public short Sequencia { get; set; }
        public StatusParada Status { get; set; }
        public DateTime? ChegadaPrevista { get; set; }

        public Guid PedidoId { get; set; }
        public int NumeroPedido { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int Volumes { get; set; }
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
    }
}
