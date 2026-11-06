using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.SaquesDto
{
    public class SolicitarSaqueDto
    {
        public decimal Valor { get; set; }
    }

    public class AtualizarChavePixDto
    {
        // CPF, CNPJ, e-mail, celular ou chave aleatória
        public string ChavePix { get; set; } = string.Empty;
    }

    public class LerSaqueDto
    {
        public Guid Id { get; set; }
        public decimal Valor { get; set; }
        public string ChavePix { get; set; } = string.Empty;
        public StatusSaque Status { get; set; }
        public string? Erro { get; set; }
        public DateTime SolicitadoEm { get; set; }
        public DateTime? PagoEm { get; set; }
    }
}
