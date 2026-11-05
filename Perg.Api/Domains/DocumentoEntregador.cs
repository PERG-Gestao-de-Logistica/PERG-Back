using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class DocumentoEntregador
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public Guid? VeiculoId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? ArquivoChave { get; set; }

    public string? Numero { get; set; }

    public string? Categoria { get; set; }

    public bool? Ear { get; set; }

    public string Status { get; set; } = null!;

    public string? MotivoRejeicao { get; set; }

    public DateOnly? Validade { get; set; }

    public DateTime? AnalisadoEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Veiculo? Veiculo { get; set; }
}
