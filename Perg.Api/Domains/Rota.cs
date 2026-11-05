using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class Rota
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid RoteirizacaoId { get; set; }

    public short Numero { get; set; }

    public string? Regiao { get; set; }

    public TipoVeiculo TipoVeiculo { get; set; }

    public Guid? EntregadorSugeridoId { get; set; }

    public Guid? EntregadorId { get; set; }

    public Guid? VeiculoId { get; set; }

    public StatusRota Status { get; set; }

    public string? Cor { get; set; }

    public DateOnly DataOperacao { get; set; }

    public int? DistanciaPrevistaM { get; set; }

    public int? DuracaoPrevistaS { get; set; }

    public decimal? CustoEstimado { get; set; }

    public string? Trajeto { get; set; }

    public DateTime? ColetaPrevistaEm { get; set; }

    public decimal? ValorEntregador { get; set; }

    public DateTime? FimPrevistoEm { get; set; }

    public bool Atrasada { get; set; }

    public int? DistanciaRealM { get; set; }

    public DateTime? IniciadaEm { get; set; }

    public DateTime? ConcluidaEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual Avaliacao? Avaliacao { get; set; }

    public virtual ICollection<CarteiraLancamento> CarteiraLancamentos { get; set; } = new List<CarteiraLancamento>();

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual Entregador? Entregador { get; set; }

    public virtual Entregador? EntregadorSugerido { get; set; }

    public virtual ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

    public virtual ICollection<OfertaRota> OfertasRota { get; set; } = new List<OfertaRota>();

    public virtual ICollection<Parada> Paradas { get; set; } = new List<Parada>();

    public virtual ICollection<PedidoEvento> PedidoEventos { get; set; } = new List<PedidoEvento>();

    public virtual Roteirizacao Roteirizacao { get; set; } = null!;

    public virtual Veiculo? Veiculo { get; set; }
}
