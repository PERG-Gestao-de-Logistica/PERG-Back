using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.RotasDto;

namespace Perg.Api.DTOs.RoteirizacoesDto
{
    public class LerRoteirizacaoDto
    {
        public Guid Id { get; set; }
        public Guid OrigemId { get; set; }
        public ObjetivoRoteirizacao Objetivo { get; set; }
        public StatusRoteirizacao Status { get; set; }
        public ResultadoRoteirizacaoDto? Resultado { get; set; }
        public string? Erro { get; set; }
        public DateTime CriadoEm { get; set; }
        public List<LerRotaDto> Rotas { get; set; } = new();
    }

    // Resumo gravado em roteirizacoes.resultado (jsonb)
    public class ResultadoRoteirizacaoDto
    {
        public int QuantidadeRotas { get; set; }
        public int PedidosRoteirizados { get; set; }
        public decimal KmTotal { get; set; }
        public long DuracaoTotalS { get; set; }
        public long RotaMaisLongaS { get; set; }

        // true = o OSRM não respondeu e as distâncias foram estimadas em linha reta
        public bool DistanciasEstimadas { get; set; }

        public List<PedidoForaDaRotaDto> PedidosForaDaRota { get; set; } = new();
    }

    public class PedidoForaDaRotaDto
    {
        public Guid PedidoId { get; set; }
        public int NumeroPedido { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
