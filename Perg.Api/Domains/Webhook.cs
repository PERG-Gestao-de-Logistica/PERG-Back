namespace Perg.Api.Domains;

// Endereço do sistema da loja que recebe os eventos dos pedidos. Tabela webhooks.
public partial class Webhook
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Url { get; set; } = null!;
    public string Segredo { get; set; } = null!;
    public bool Ativo { get; set; }
    public int FalhasSeguidas { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
