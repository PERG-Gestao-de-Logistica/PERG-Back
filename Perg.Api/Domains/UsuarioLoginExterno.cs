using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class UsuarioLoginExterno
{
    public string Provedor { get; set; } = null!;

    public string ChaveProvedor { get; set; } = null!;

    public Guid UsuarioId { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
