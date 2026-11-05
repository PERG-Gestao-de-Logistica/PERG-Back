using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class OfertaRota
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid RotaId { get; set; }

    public Guid EntregadorId { get; set; }

    public RodadaOferta Rodada { get; set; }

    public StatusOferta Status { get; set; }

    public DateTime EnviadaEm { get; set; }

    public DateTime ExpiraEm { get; set; }

    public DateTime? RespondidaEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota Rota { get; set; } = null!;
}
