using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EntregadoresDto
{
    // Perfil do próprio entregador (app)
    public class LerEntregadorDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public StatusEntregador Status { get; set; }
        public TipoVeiculo? VeiculoTipo { get; set; }
        public string? VeiculoPlaca { get; set; }
    }

    // O que a loja vê de um entregador da rede (sem CPF, telefone, endereço ou documentos)
    public class LerEntregadorRedeDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? FotoChave { get; set; }
        public TipoVeiculo? VeiculoTipo { get; set; }
        public string? VeiculoPlaca { get; set; }
        public bool VeiculoVerificado { get; set; }
        public bool CnhVerificada { get; set; }
        public long TotalEntregas { get; set; }

        // entregas ÷ (entregas + falhas sem culpa do cliente); null se ainda não entregou nada
        public decimal? TaxaSucesso { get; set; }
        public decimal? NotaMedia { get; set; }
        public long TotalAvaliacoes { get; set; }
        public decimal? TempoAceiteMedioS { get; set; }
        public bool Favorito { get; set; }
    }
}
