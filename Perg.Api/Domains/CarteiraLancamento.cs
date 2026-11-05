using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class CarteiraLancamento
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public string Tipo { get; set; } = null!;

    public decimal Valor { get; set; }

    public Guid? RotaId { get; set; }

    public Guid? SaqueId { get; set; }

    public string? Descricao { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota? Rota { get; set; }

    public virtual Saque? Saque { get; set; }
}
