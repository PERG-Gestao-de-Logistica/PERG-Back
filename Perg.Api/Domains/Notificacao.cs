using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class Notificacao
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid UsuarioId { get; set; }

    public TipoNotificacao Tipo { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Mensagem { get; set; }

    public string? Link { get; set; }

    public DateTime? LidaEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
