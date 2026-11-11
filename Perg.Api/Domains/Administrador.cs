namespace Perg.Api.Domains;

// Equipe interna do Perg (backoffice). Tabela administradores.
public partial class Administrador
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string SenhaHash { get; set; } = null!;

    public bool TrocarSenha { get; set; }

    public bool Ativo { get; set; }

    public DateTime? UltimoAcessoEm { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
