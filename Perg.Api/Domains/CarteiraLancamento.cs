using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class CarteiraLancamento
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public TipoLancamento Tipo { get; set; }

    public decimal Valor { get; set; }

    public Guid? RotaId { get; set; }

    public Guid? SaqueId { get; set; }

    public string? Descricao { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota? Rota { get; set; }

    public virtual Saque? Saque { get; set; }
}
