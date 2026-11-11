using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.BackofficeDto
{
    public class LoginAdministradorDto
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class CriarAdministradorDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class LerAdministradorDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public bool TrocarSenha { get; set; }
        public DateTime? UltimoAcessoEm { get; set; }
    }

    public class MotivoDto
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public class TrocarSenhaAdministradorDto
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    public class LerEntregadorResumoBackofficeDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public StatusEntregador Status { get; set; }
        public TipoVeiculo? TipoVeiculo { get; set; }
        public int DocumentosPendentes { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    public class LerDocumentoBackofficeDto
    {
        public Guid Id { get; set; }
        public TipoDocumento Tipo { get; set; }
        public StatusDocumento Status { get; set; }
        public string? Numero { get; set; }
        public string? Categoria { get; set; }
        public bool? Ear { get; set; }
        public DateOnly? Validade { get; set; }
        public string? MotivoRejeicao { get; set; }
        public DateTime? AnalisadoEm { get; set; }
        public string? UrlArquivo { get; set; }   // GET com o token do backoffice
        public DateTime CriadoEm { get; set; }
    }

    public class LerEntregadorBackofficeDto : LerEntregadorResumoBackofficeDto
    {
        public string Email { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public string? Placa { get; set; }
        public string? ModeloVeiculo { get; set; }
        public DateTime? AprovadoEm { get; set; }
        public List<LerDocumentoBackofficeDto> Documentos { get; set; } = new();

        // O que ainda impede a aprovação (vazio = pode aprovar)
        public List<string> Pendencias { get; set; } = new();
    }
}
