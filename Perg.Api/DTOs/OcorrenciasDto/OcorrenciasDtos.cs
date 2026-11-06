using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.OcorrenciasDto
{
    public class LerOcorrenciaDto
    {
        public Guid Id { get; set; }
        public TipoOcorrencia Tipo { get; set; }
        public StatusOcorrencia Status { get; set; }
        public string? Descricao { get; set; }
        public string? FotoChave { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime RegistradaEm { get; set; }
        public DateTime? ResolvidaEm { get; set; }
        public Guid PedidoId { get; set; }
        public int NumeroPedido { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public Guid RotaId { get; set; }
        public string? EntregadorNome { get; set; }
    }

    // O que a loja decide fazer com o pedido que não foi entregue
    public enum AcaoOcorrencia
    {
        // O pedido volta para "novo" e entra na próxima roteirização
        Reentregar = 1,
        Cancelar = 2
    }

    public class ResolverOcorrenciaDto
    {
        public AcaoOcorrencia Acao { get; set; }
        public string? Observacao { get; set; }
    }
}
