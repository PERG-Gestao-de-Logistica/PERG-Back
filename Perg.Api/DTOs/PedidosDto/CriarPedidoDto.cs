using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PedidosDto
{
    public class CriarPedidoDto
    {
        // Se não vier, usa a origem padrão da empresa
        public Guid? OrigemId { get; set; }

        public string? NumeroExterno { get; set; }
        public ModalidadePedido Modalidade { get; set; } = ModalidadePedido.MesmoDia;

        public string ClienteNome { get; set; } = string.Empty;
        public string? ClienteTelefone { get; set; }
        public string? ClienteEmail { get; set; }

        // Obrigatório, menos na retirada na loja
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Referencia { get; set; }

        // Janela de entrega (obrigatória na entrega agendada)
        public DateTimeOffset? JanelaInicio { get; set; }
        public DateTimeOffset? JanelaFim { get; set; }

        public decimal PesoKg { get; set; }
        public int Volumes { get; set; } = 1;
        public string? Observacoes { get; set; }
    }
}
