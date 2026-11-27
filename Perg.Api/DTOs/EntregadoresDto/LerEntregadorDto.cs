using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EntregadoresDto
{
    public class LerEntregadorDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public StatusEntregador Status { get; set; }
        public bool TelefoneConfirmado { get; set; }
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

        // Só com esta loja (tela Entregadores: "entregas com você")
        public long EntregasComLoja { get; set; }
        public decimal? TaxaSucessoComLoja { get; set; }

        // Agora: disponível (jornada aberta), em rota desta loja, em rota de outra loja ou offline
        public SituacaoEntregadorRede Situacao { get; set; }

        // Linha reta da última posição (até 10 min) ao ponto de coleta padrão; null se offline ou sem posição
        public decimal? DistanciaOrigemKm { get; set; }
    }

    public enum SituacaoEntregadorRede
    {
        Disponivel = 1,
        EmRota = 2,
        OutraLoja = 3,
        Offline = 4
    }
}
