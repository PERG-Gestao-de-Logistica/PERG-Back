using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class Empresa
{
    public Guid Id { get; set; }

    public string Cnpj { get; set; } = null!;

    public string RazaoSocial { get; set; } = null!;

    public string NomeFantasia { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Telefone { get; set; }

    public string? FotoChave { get; set; }

    public SegmentoEmpresa Segmento { get; set; }

    public string FaixaPedidosDia { get; set; } = null!;

    public bool EntregaMesmoDia { get; set; }

    public bool EntregaAgendada { get; set; }

    public bool RetiradaLoja { get; set; }

    public bool VendeSiteProprio { get; set; }

    public bool VendeWhatsapp { get; set; }

    public bool VendeShopify { get; set; }

    public bool UsaCarro { get; set; }

    public bool UsaMoto { get; set; }

    public bool UsaVanCaminhao { get; set; }

    public bool SomenteFavoritos { get; set; }

    public int TempoOfertaFavoritosS { get; set; }

    public string FusoHorario { get; set; } = null!;

    public decimal MetaNoPrazoPct { get; set; }

    public StatusEmpresa Status { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();

    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();

    public virtual ICollection<EmpresaFavorito> EmpresaFavoritos { get; set; } = new List<EmpresaFavorito>();

    public virtual ICollection<Importacao> Importacoes { get; set; } = new List<Importacao>();

    public virtual ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

    public virtual ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();

    public virtual ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();

    public virtual ICollection<OfertaRota> OfertasRota { get; set; } = new List<OfertaRota>();

    public virtual ICollection<Origem> Origens { get; set; } = new List<Origem>();

    public virtual ICollection<Parada> Paradas { get; set; } = new List<Parada>();

    public virtual ICollection<PedidoEvento> PedidoEventos { get; set; } = new List<PedidoEvento>();

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual ICollection<Rota> Rotas { get; set; } = new List<Rota>();

    public virtual ICollection<Roteirizacao> Roteirizacoes { get; set; } = new List<Roteirizacao>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
