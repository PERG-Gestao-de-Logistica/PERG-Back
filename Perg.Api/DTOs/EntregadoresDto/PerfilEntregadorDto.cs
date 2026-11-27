using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EntregadoresDto
{
    // GET /api/app/entregadores/eu/perfil: tudo o que a tela "Meu perfil" do app mostra, numa chamada
    public class LerPerfilEntregadorDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? FotoChave { get; set; }
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public StatusEntregador Status { get; set; }
        public DateTime MembroDesde { get; set; }

        public VeiculoPerfilDto? Veiculo { get; set; }

        // Desempenho em todas as lojas
        public long TotalEntregas { get; set; }
        public long TotalFalhas { get; set; }          // só as que contam (sem culpa do cliente)
        public decimal? TaxaSucesso { get; set; }      // 0 a 1; null se ainda não entregou nada
        public decimal? NotaMedia { get; set; }        // média das avaliações das lojas (1 a 5)
        public long TotalAvaliacoes { get; set; }
        public int? TempoAceiteMedioMin { get; set; }

        // Documentos
        public bool CnhVerificada { get; set; }        // CNH aprovada e dentro da validade
        public bool Verificado { get; set; }           // CNH + veículo verificados: selo "verificado pela Perg"
        public List<DocumentoPerfilDto> Documentos { get; set; } = new();
    }

    public class VeiculoPerfilDto
    {
        public TipoVeiculo Tipo { get; set; }
        public string? Placa { get; set; }
        public string? Modelo { get; set; }
        public bool Verificado { get; set; }
    }

    public class DocumentoPerfilDto
    {
        public TipoDocumento Tipo { get; set; }
        public StatusDocumento Status { get; set; }
        public string? Categoria { get; set; }
        public DateOnly? Validade { get; set; }
        public bool Vencido { get; set; }
        public string? MotivoRejeicao { get; set; }
    }
}
