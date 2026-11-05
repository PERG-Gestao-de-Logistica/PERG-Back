using System;
using System.Collections.Generic;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

public partial class Usuario
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Telefone { get; set; }

    public PapelUsuario Papel { get; set; }

    public string? SenhaHash { get; set; }

    public bool TrocarSenha { get; set; }

    public bool EmailConfirmado { get; set; }

    public string? FotoChave { get; set; }

    public bool Ativo { get; set; }

    public bool NotifOcorrencias { get; set; }

    public bool NotifCobranca { get; set; }

    public string? TermosVersao { get; set; }

    public DateTime? TermosAceitosEm { get; set; }

    public DateTime? UltimoAcessoEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public virtual ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();

    public virtual Empresa Empresa { get; set; } = null!;

    public virtual ICollection<Importacao> Importacoes { get; set; } = new List<Importacao>();

    public virtual ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

    public virtual ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();

    public virtual ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();

    public virtual ICollection<Origem> Origens { get; set; } = new List<Origem>();

    public virtual ICollection<Roteirizacao> Roteirizacoes { get; set; } = new List<Roteirizacao>();

    public virtual ICollection<UsuarioLoginExterno> UsuariosLoginsExternos { get; set; } = new List<UsuarioLoginExterno>();
}
