namespace Perg.Api.Domains;

// Um envio de evento para um webhook (com novas tentativas). Tabela webhook_entregas.
public partial class WebhookEntrega
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public Guid EmpresaId { get; set; }
    public string Evento { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Tentativas { get; set; }
    public DateTime ProximaTentativaEm { get; set; }
    public int? UltimoStatusHttp { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime? EntregueEm { get; set; }
    public DateTime CriadoEm { get; set; }

    public virtual Webhook Webhook { get; set; } = null!;
}
