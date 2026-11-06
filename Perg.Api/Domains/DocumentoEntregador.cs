using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class DocumentoEntregador
{
    public Guid Id { get; set; }

    public Guid EntregadorId { get; set; }

    public Guid? VeiculoId { get; set; }

    public TipoDocumento Tipo { get; set; }

    public string? ArquivoChave { get; set; }

    public string? Numero { get; set; }

    public string? Categoria { get; set; }

    public bool? Ear { get; set; }

    public StatusDocumento Status { get; set; }

    public string? MotivoRejeicao { get; set; }

    public DateOnly? Validade { get; set; }

    public DateTime? AnalisadoEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public virtual Entregador Entregador { get; set; } = null!;

    public virtual Veiculo? Veiculo { get; set; }
}
