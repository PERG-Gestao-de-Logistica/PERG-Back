using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Jornada
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public Guid? VeiculoId { get; set; }

    public DateTime IniciadaEm { get; set; }

    public DateTime? EncerradaEm { get; set; }

    public int? DistanciaM { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Veiculo? Veiculo { get; set; }
}
