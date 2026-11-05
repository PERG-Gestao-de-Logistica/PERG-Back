using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class PosicaoEntregador
{
    public Guid EntregadorId { get; set; }

    public DateTime RegistradaEm { get; set; }

    public Guid? EmpresaId { get; set; }

    public Guid? RotaId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal? PrecisaoM { get; set; }

    public decimal? VelocidadeKmh { get; set; }

    public DateTime RecebidaEm { get; set; }
}
