using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class OfertaRota
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid RotaId { get; set; }

    public Guid EntregadorId { get; set; }

    public string Rodada { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime EnviadaEm { get; set; }

    public DateTime ExpiraEm { get; set; }

    public DateTime? RespondidaEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota Rota { get; set; } = null!;
}
