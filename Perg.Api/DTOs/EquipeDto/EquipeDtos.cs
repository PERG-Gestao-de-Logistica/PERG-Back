using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EquipeDto
{
    public class CriarMembroDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public PapelUsuario Papel { get; set; } = PapelUsuario.Funcionario;
    }

    public class AtualizarMembroDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public PapelUsuario Papel { get; set; }
    }

    public class LerMembroDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public PapelUsuario Papel { get; set; }
        public bool Ativo { get; set; }

        // true = ainda não trocou a senha provisória (primeiro acesso pendente)
        public bool PrimeiroAcessoPendente { get; set; }

        public DateTime? UltimoAcessoEm { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    // Resposta de criar membro / redefinir senha: a senha provisória aparece só aqui, uma única vez
    public class MembroComSenhaDto
    {
        public LerMembroDto Membro { get; set; } = new();
        public string SenhaTemporaria { get; set; } = string.Empty;
    }
}
