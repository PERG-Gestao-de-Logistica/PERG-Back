using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Ocorrencia
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid ParadaId { get; set; }

    public Guid? EntregadorId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? Descricao { get; set; }

    public string? FotoChave { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string Status { get; set; } = null!;

    public Guid? ResolvidaPor { get; set; }

    public DateTime? ResolvidaEm { get; set; }

    public DateTime RegistradaEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador? Entregador { get; set; }

    public virtual Parada Parada { get; set; } = null!;

    public virtual Usuario? ResolvidaPorNavigation { get; set; }
}
