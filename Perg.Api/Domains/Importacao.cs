using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Importacao
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid OrigemId { get; set; }

    public string ArquivoNome { get; set; } = null!;

    public string ArquivoChave { get; set; } = null!;

    public string? MapeamentoColunas { get; set; }

    public string Status { get; set; } = null!;

    public int? TotalLinhas { get; set; }

    public int LinhasImportadas { get; set; }

    public int LinhasComErro { get; set; }

    public string? Erros { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? ConcluidaEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Origem Origem { get; set; } = null!;

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual Usuario Usuario { get; set; } = null!;
}
