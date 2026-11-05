using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EmpresasDto
{
    public class LerEmpresaDto
    {
        public Guid Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public SegmentoEmpresa Segmento { get; set; }
        public StatusEmpresa Status { get; set; }
    }
}
