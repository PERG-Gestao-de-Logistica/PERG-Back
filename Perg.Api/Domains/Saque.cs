using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Saque
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public decimal Valor { get; set; }

    public string ChavePix { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? GatewayTransferenciaId { get; set; }

    public string? Erro { get; set; }

    public DateTime SolicitadoEm { get; set; }

    public DateTime? PagoEm { get; set; }

    public virtual ICollection<CarteiraLancamento> CarteiraLancamentos { get; set; } = new List<CarteiraLancamento>();

    public virtual Entregador Entregador { get; set; } = null!;
}
