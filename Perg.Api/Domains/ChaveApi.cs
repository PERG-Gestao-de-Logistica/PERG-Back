namespace Perg.Api.Domains;

// Chave de API de uma loja para a integração com sistemas de vendas. Tabela chaves_api.
public partial class ChaveApi
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Nome { get; set; } = null!;
    public string Prefixo { get; set; } = null!;
    public string HashChave { get; set; } = null!;
    public Guid? CriadoPor { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? UltimoUsoEm { get; set; }
    public DateTime? RevogadaEm { get; set; }
}
