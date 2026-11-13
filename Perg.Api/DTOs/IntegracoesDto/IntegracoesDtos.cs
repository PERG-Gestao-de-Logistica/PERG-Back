using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.IntegracoesDto
{
    // ---------- API pública (sistemas de vendas) ----------

    public class ClienteIntegracaoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
    }

    public class EnderecoIntegracaoDto
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Referencia { get; set; }
    }

    public class CriarPedidoIntegracaoDto
    {
        // Id do pedido no sistema de vendas. Mandar o mesmo id de novo não duplica: devolve o pedido já criado.
        public string IdExterno { get; set; } = string.Empty;
        public string? NumeroExterno { get; set; }
        public Guid? OrigemId { get; set; }
        public ModalidadePedido Modalidade { get; set; } = ModalidadePedido.MesmoDia;
        public ClienteIntegracaoDto Cliente { get; set; } = new();
        public EnderecoIntegracaoDto Endereco { get; set; } = new();
        public DateTimeOffset? JanelaInicio { get; set; }
        public DateTimeOffset? JanelaFim { get; set; }
        public decimal PesoKg { get; set; }
        public int Volumes { get; set; } = 1;
        public string? Observacoes { get; set; }
    }

    public class CancelarPedidoIntegracaoDto
    {
        public string? Motivo { get; set; }
    }

    public class LerPedidoIntegracaoDto
    {
        public Guid Id { get; set; }
        public string? IdExterno { get; set; }
        public int NumeroPedido { get; set; }
        public string? NumeroExterno { get; set; }
        public StatusPedido Status { get; set; }
        public ModalidadePedido Modalidade { get; set; }
        public StatusGeocodificacao GeocodificacaoStatus { get; set; }
        public string LinkRastreio { get; set; } = string.Empty;
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
        public DateTime? EntregueEm { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    // ---------- Gestão pela loja (portal) ----------

    public class CriarChaveApiDto
    {
        public string Nome { get; set; } = string.Empty;
    }

    public class LerChaveApiDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Inicio { get; set; } = string.Empty;   // "perg_Ab12Cd..."
        public bool Ativa { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? UltimoUsoEm { get; set; }
    }

    public class SalvarWebhookDto
    {
        public string Url { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
    }

    public class LerWebhookDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int FalhasSeguidas { get; set; }
        public DateTime CriadoEm { get; set; }
        public string? Segredo { get; set; }   // só na criação
    }

    public class LerWebhookEntregaDto
    {
        public Guid Id { get; set; }
        public string Evento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Tentativas { get; set; }
        public int? UltimoStatusHttp { get; set; }
        public string? UltimoErro { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? EntregueEm { get; set; }
        public DateTime ProximaTentativaEm { get; set; }
    }
}
