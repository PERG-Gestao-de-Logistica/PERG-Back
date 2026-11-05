using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Origem
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = null!;

    public Guid? ResponsavelId { get; set; }

    public string Cep { get; set; } = null!;

    public string Logradouro { get; set; } = null!;

    public string Numero { get; set; } = null!;

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = null!;

    public string Cidade { get; set; } = null!;

    public string Uf { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public TimeOnly Abre { get; set; }

    public TimeOnly Fecha { get; set; }

    public bool AbreSabado { get; set; }

    public bool AbreDomingo { get; set; }

    public bool Padrao { get; set; }

    public bool Ativa { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual ICollection<Importacao> Importacoes { get; set; } = new List<Importacao>();

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual Usuario? Responsavel { get; set; }

    public virtual ICollection<Roteirizacao> Roteirizacoes { get; set; } = new List<Roteirizacao>();
}
