using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.OrigensDto
{
    // Usado para criar (POST) e para editar (PUT /api/Origens/{id})
    public class CriarOrigemDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoOrigem Tipo { get; set; } = TipoOrigem.Loja;

        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;

        // Ponto ajustado no mapa. Sem ele, a API calcula pelo endereço.
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? ResponsavelNome { get; set; }
        public string? ResponsavelTelefone { get; set; }

        public TimeOnly Abre { get; set; }
        public TimeOnly Fecha { get; set; }

        // Dias abertos: 0 domingo ... 6 sábado. Sem a lista, vale seg a sex + AbreSabado/AbreDomingo.
        public List<int>? Dias { get; set; }
        public bool AbreSabado { get; set; }
        public bool AbreDomingo { get; set; }

        public string? InstrucoesColeta { get; set; }

        // Marca como origem padrão (a anterior deixa de ser)
        public bool Padrao { get; set; }
    }

    // POST /api/Origens/localizar: endereço -> ponto no mapa, antes de salvar
    public class LocalizarOrigemDto
    {
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
    }
}
