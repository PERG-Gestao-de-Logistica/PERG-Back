using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Outbox
{
    public Guid Id { get; set; }

    public string Topico { get; set; } = null!;

    public string Chave { get; set; } = null!;

    public string TipoEvento { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime CriadoEm { get; set; }

    public DateTime? PublicadoEm { get; set; }
}
