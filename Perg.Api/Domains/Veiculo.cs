using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class Veiculo
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public TipoVeiculo Tipo { get; set; }

    public string? Placa { get; set; }

    public string? Modelo { get; set; }

    public decimal CapacidadeKg { get; set; }

    public int? CapacidadeVolumes { get; set; }

    public int? CapacidadeLitros { get; set; }

    public bool Verificado { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual ICollection<DocumentoEntregador> DocumentosEntregador { get; set; } = new List<DocumentoEntregador>();

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual ICollection<Entregador> Entregadores { get; set; } = new List<Entregador>();

    public virtual ICollection<Jornada> Jornadas { get; set; } = new List<Jornada>();

    public virtual ICollection<Rota> Rotas { get; set; } = new List<Rota>();
}
