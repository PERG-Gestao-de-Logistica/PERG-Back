using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class EmpresaFavorito
{
    public Guid EmpresaId { get; set; }

    public Guid EntregadorId { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador Entregador { get; set; } = null!;
}
