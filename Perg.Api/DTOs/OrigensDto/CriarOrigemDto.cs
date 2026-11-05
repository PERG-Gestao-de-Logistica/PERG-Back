namespace Perg.Api.DTOs.OrigensDto
{
    public class CriarOrigemDto
    {
        public string Nome { get; set; } = string.Empty;
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

        // Marca como origem padrão (a anterior deixa de ser)
        public bool Padrao { get; set; }
    }
}
