using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Avaliacao
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid EntregadorId { get; set; }

    public Guid RotaId { get; set; }

    public short Nota { get; set; }

    public string? Comentario { get; set; }

    public Guid? AvaliadoPor { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Usuario? AvaliadoPorNavigation { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota Rota { get; set; } = null!;
}
