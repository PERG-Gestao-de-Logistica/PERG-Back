using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Mensagem
{
    public Guid Id { get; set; }

    public Guid? EmpresaId { get; set; }

    public Guid EntregadorId { get; set; }

    public Guid? RotaId { get; set; }

    public string AutorTipo { get; set; } = null!;

    public Guid? AutorUsuarioId { get; set; }

    public string Texto { get; set; } = null!;

    public DateTime EnviadaEm { get; set; }

    public DateTime? LidaEm { get; set; }

    public virtual Usuario? AutorUsuario { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Rota? Rota { get; set; }
}
