using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EmpresasDto
{
    // GET /api/Empresas/eu: dados da empresa para a aba Configurações > Empresa
    public class LerConfiguracaoEmpresaDto
    {
        public Guid Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public SegmentoEmpresa Segmento { get; set; }
        public string FaixaPedidosDia { get; set; } = string.Empty;

        public bool EntregaMesmoDia { get; set; }
        public bool EntregaAgendada { get; set; }
        public bool RetiradaLoja { get; set; }

        public bool UsaCarro { get; set; }
        public bool UsaMoto { get; set; }
        public bool UsaVanCaminhao { get; set; }

        public bool SomenteFavoritos { get; set; }
        public int TempoOfertaFavoritosS { get; set; }
        public decimal MetaNoPrazoPct { get; set; }
    }

    // PUT /api/Empresas/eu (só admin). O CNPJ não muda.
    public class AtualizarEmpresaDto
    {
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public SegmentoEmpresa Segmento { get; set; }
        public string FaixaPedidosDia { get; set; } = string.Empty;

        public bool EntregaMesmoDia { get; set; }
        public bool EntregaAgendada { get; set; }
        public bool RetiradaLoja { get; set; }

        public bool UsaCarro { get; set; }
        public bool UsaMoto { get; set; }
        public bool UsaVanCaminhao { get; set; }

        public bool SomenteFavoritos { get; set; }
        public int TempoOfertaFavoritosS { get; set; } = 120;
        public decimal MetaNoPrazoPct { get; set; } = 95;
    }
}
