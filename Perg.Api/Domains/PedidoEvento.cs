using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class PedidoEvento
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid PedidoId { get; set; }

    public string TipoEvento { get; set; } = null!;

    public string? StatusNovo { get; set; }

    public string Descricao { get; set; } = null!;

    public string AutorTipo { get; set; } = null!;

    public Guid? AutorId { get; set; }

    public Guid? RotaId { get; set; }

    public string? Dados { get; set; }

    public DateTime OcorridoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Pedido Pedido { get; set; } = null!;

    public virtual Rota? Rota { get; set; }
}
