using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class MensagemProcessada
{
    public string Consumidor { get; set; } = null!;

    public Guid MensagemId { get; set; }

    public DateTime ProcessadaEm { get; set; }
}
