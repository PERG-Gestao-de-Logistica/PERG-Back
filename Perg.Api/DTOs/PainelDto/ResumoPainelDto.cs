using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.PainelDto
{
    // Tudo o que a visão geral da loja mostra, numa chamada só
    public class ResumoPainelDto
    {
        public PrimeirosPassosDto PrimeirosPassos { get; set; } = new();
        public int PedidosHoje { get; set; }
        public int RotasAtivas { get; set; }
        public int RotasEmAndamento { get; set; }
        // Entregas de hoje concluídas até 15 min depois do previsto; null sem entregas concluídas
        public decimal? NoPrazoPercentual { get; set; }
        public decimal KmRodados { get; set; }
        public List<RotaHojeDto> RotasHoje { get; set; } = new();
        public List<OcorrenciaAbertaDto> OcorrenciasAbertas { get; set; } = new();
    }

    public class PrimeirosPassosDto
    {
        public bool ContaCriada { get; set; } = true;
        public bool PontoColetaCadastrado { get; set; }
        public bool EntregadorFavorito { get; set; }
        public bool PedidoCriado { get; set; }
    }

    public class RotaHojeDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? EntregadorNome { get; set; }
        public int Paradas { get; set; }
        public string? Regiao { get; set; }
        public TipoVeiculo TipoVeiculo { get; set; }
        public StatusRota Status { get; set; }
        public int Progresso { get; set; }
    }

    public class OcorrenciaAbertaDto
    {
        public Guid Id { get; set; }
        public int NumeroPedido { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public TipoOcorrencia Tipo { get; set; }
        public string? EntregadorNome { get; set; }
        public DateTime RegistradaEm { get; set; }
    }
}
