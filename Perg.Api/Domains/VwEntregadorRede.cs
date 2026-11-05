using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class VwEntregadorRede
{
    public Guid? Id { get; set; }

    public string? Nome { get; set; }

    public string? FotoChave { get; set; }

    public string? VeiculoTipo { get; set; }

    public string? VeiculoPlaca { get; set; }

    public bool? VeiculoVerificado { get; set; }

    public bool? CnhVerificada { get; set; }

    public long? TotalEntregas { get; set; }

    public long? TotalFalhas { get; set; }

    public decimal? NotaMedia { get; set; }

    public long? TotalAvaliacoes { get; set; }

    public decimal? TempoAceiteMedioS { get; set; }
}
