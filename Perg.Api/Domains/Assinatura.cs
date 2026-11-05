using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Assinatura
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid PlanoId { get; set; }

    public short Status { get; set; }

    public string? Gateway { get; set; }

    public string? GatewayAssinaturaId { get; set; }

    public DateOnly? ProximoVencimento { get; set; }

    public DateTime? CanceladaEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Plano Plano { get; set; } = null!;
}
