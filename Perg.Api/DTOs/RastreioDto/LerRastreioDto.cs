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
        public List<LerRastreioEventoDto> Eventos { get; set; } = new();
    }

    public class LerRastreioEventoDto
    {
        public string Descricao { get; set; } = string.Empty;
        public StatusPedido? StatusNovo { get; set; }
        public DateTime OcorridoEm { get; set; }
    }
}
