using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EntregadoresDto
{
    // Cadastro pelo app. Na versão final, telefone e e-mail são confirmados por código (SMS/e-mail)
    // antes desta chamada; por enquanto o cadastro entra "em análise".
    public class CadastrarEntregadorDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;

        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;

        public string? ChavePix { get; set; }

        // Inclui o consentimento de localização (LGPD)
        public bool AceitouTermos { get; set; }

        public CadastrarVeiculoDto Veiculo { get; set; } = new();
    }

    public class CadastrarVeiculoDto
    {
        public TipoVeiculo Tipo { get; set; }

        // Obrigatória, menos para bike
        public string? Placa { get; set; }
        public string? Modelo { get; set; }
    }
}
