using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EmpresasDto
{
    public class CadastrarEmpresaDto
    {
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

        public bool VendeSiteProprio { get; set; }
        public bool VendeWhatsapp { get; set; }
        public bool VendeShopify { get; set; }

        public bool UsaCarro { get; set; }
        public bool UsaMoto { get; set; }
        public bool UsaVanCaminhao { get; set; }

        public CadastrarResponsavelDto Responsavel { get; set; } = new();
        public CadastrarEnderecoDto Endereco { get; set; } = new();
    }

    // Vira o primeiro usuário da loja, com papel admin
    public class CadastrarResponsavelDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string Senha { get; set; } = string.Empty;
        public bool AceitouTermos { get; set; }
    }

    // Vira a origem padrão (ponto de coleta)
    public class CadastrarEnderecoDto
    {
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public TimeOnly Abre { get; set; }
        public TimeOnly Fecha { get; set; }
        public bool AbreSabado { get; set; }
        public bool AbreDomingo { get; set; }
    }
}
