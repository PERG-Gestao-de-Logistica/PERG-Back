using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Roteirizacao
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid OrigemId { get; set; }

    public Guid? SolicitadaPor { get; set; }

    public string Objetivo { get; set; } = null!;

    public string Parametros { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Resultado { get; set; }

    public string? Erro { get; set; }

    public DateTime? IniciadaEm { get; set; }

    public DateTime? ConcluidaEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Origem Origem { get; set; } = null!;

    public virtual ICollection<Rota> Rotas { get; set; } = new List<Rota>();

    public virtual Usuario? SolicitadaPorNavigation { get; set; }
}
