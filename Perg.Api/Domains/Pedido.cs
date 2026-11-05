using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Pedido
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid OrigemId { get; set; }

    public Guid? ImportacaoId { get; set; }

    public string Canal { get; set; } = null!;

    public string? IdExterno { get; set; }

    public int NumeroPedido { get; set; }

    public string? NumeroExterno { get; set; }

    public string Modalidade { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string ClienteNome { get; set; } = null!;

    public string? ClienteTelefone { get; set; }

    public string? ClienteEmail { get; set; }

    public string? Cep { get; set; }

    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Uf { get; set; }

    public string? Referencia { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string GeocodificacaoStatus { get; set; } = null!;

    public decimal? GeocodificacaoConfianca { get; set; }

    public DateTime? JanelaInicio { get; set; }

    public DateTime? JanelaFim { get; set; }

    public decimal PesoKg { get; set; }

    public int Volumes { get; set; }

    public string TokenRastreamento { get; set; } = null!;

    public DateTime? TokenExpiraEm { get; set; }

    public string? Observacoes { get; set; }

    public string? DadosOriginais { get; set; }

    public DateTime? EntregueEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Importacao? Importacao { get; set; }

    public virtual Origem Origem { get; set; } = null!;

    public virtual ICollection<Parada> Paradas { get; set; } = new List<Parada>();

    public virtual ICollection<PedidoEvento> PedidoEventos { get; set; } = new List<PedidoEvento>();
}
