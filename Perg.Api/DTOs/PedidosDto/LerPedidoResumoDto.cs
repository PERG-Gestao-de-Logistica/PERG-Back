using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PedidosDto
{
    public class LerPedidoResumoDto
    {
        public Guid Id { get; set; }
        public int NumeroPedido { get; set; }
        public string? NumeroExterno { get; set; }
        public StatusPedido Status { get; set; }
        public ModalidadePedido Modalidade { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
        public int Volumes { get; set; }
        public decimal PesoKg { get; set; }
        public StatusGeocodificacao GeocodificacaoStatus { get; set; }
        public DateTime CriadoEm { get; set; }

        // Diagnóstico para a tela: pronto para a roteirização de hoje, o que impede e avisos
        public bool Pronto { get; set; }
        public List<string> Problemas { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }
}
