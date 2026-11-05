using System;
using System.Collections.Generic;

namespace Perg.Api.Domains;

public partial class Entregador
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Cpf { get; set; } = null!;

    public string Telefone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? FotoChave { get; set; }

    public string Cep { get; set; } = null!;

    public string Logradouro { get; set; } = null!;

    public string Numero { get; set; } = null!;

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = null!;

    public string Cidade { get; set; } = null!;

    public string Uf { get; set; } = null!;

    public string? ChavePix { get; set; }

    public string SenhaHash { get; set; } = null!;

    public DateTime? SenhaAlteradaEm { get; set; }

    public bool TelefoneConfirmado { get; set; }

    public bool EmailConfirmado { get; set; }

    public string? TermosVersao { get; set; }

    public DateTime? TermosAceitosEm { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? AprovadoEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public Guid? VeiculoAtualId { get; set; }

    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();

    public virtual ICollection<CarteiraLancamento> CarteiraLancamentos { get; set; } = new List<CarteiraLancamento>();

    public virtual ICollection<DocumentoEntregador> DocumentosEntregador { get; set; } = new List<DocumentoEntregador>();

    public virtual ICollection<EmpresaFavorito> EmpresaFavoritos { get; set; } = new List<EmpresaFavorito>();

    public virtual ICollection<Jornada> Jornadas { get; set; } = new List<Jornada>();

    public virtual ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

    public virtual ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();

    public virtual ICollection<OfertaRota> OfertasRota { get; set; } = new List<OfertaRota>();

    public virtual ICollection<Rota> RotasEntregador { get; set; } = new List<Rota>();

    public virtual ICollection<Rota> RotasEntregadorSugerido { get; set; } = new List<Rota>();

    public virtual ICollection<Saque> Saques { get; set; } = new List<Saque>();

    public virtual Veiculo? VeiculoAtual { get; set; }

    public virtual ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
}
