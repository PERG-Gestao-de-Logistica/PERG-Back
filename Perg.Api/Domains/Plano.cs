using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Plano
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = null!;

    public decimal PrecoMensal { get; set; }

    public int? LimitePedidosMes { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}
