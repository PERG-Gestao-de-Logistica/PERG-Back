using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Parada
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid RotaId { get; set; }

    public Guid PedidoId { get; set; }

    public short Sequencia { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ChegadaPrevista { get; set; }

    public DateTime? ChegadaReal { get; set; }

    public DateTime? ConcluidaEm { get; set; }

    public string? ComprovanteTipo { get; set; }

    public string? ComprovanteChave { get; set; }

    public string? RecebedorNome { get; set; }

    public decimal? LatitudeConclusao { get; set; }

    public decimal? LongitudeConclusao { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();

    public virtual Pedido Pedido { get; set; } = null!;

    public virtual Rota Rota { get; set; } = null!;
}
