using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.OfertasDto
{
    public class OfertarRotaDto
    {
        // Se não vier, usa o valor calculado na roteirização
        public decimal? ValorEntregador { get; set; }
    }

    // O que aparece no app do entregador
    public class LerOfertaDto
    {
        public Guid Id { get; set; }
        public Guid RotaId { get; set; }
        public string CodigoRota { get; set; } = string.Empty;
        public string NomeLoja { get; set; } = string.Empty;
        public string EnderecoColeta { get; set; } = string.Empty;
        public decimal? ColetaLatitude { get; set; }
        public decimal? ColetaLongitude { get; set; }
        public TipoVeiculo TipoVeiculo { get; set; }
        public int QuantidadeEntregas { get; set; }
        public List<string> Bairros { get; set; } = new();
        public int? DistanciaPrevistaM { get; set; }
        public int? DuracaoPrevistaS { get; set; }
        public DateTime? ColetaPrevistaEm { get; set; }
        public decimal? ValorEntregador { get; set; }
        public RodadaOferta Rodada { get; set; }
        public DateTime ExpiraEm { get; set; }
    }

    // O que a loja vê das ofertas de uma rota
    public class LerOfertaLojaDto
    {
        public Guid Id { get; set; }
        public Guid EntregadorId { get; set; }
        public string EntregadorNome { get; set; } = string.Empty;
        public RodadaOferta Rodada { get; set; }
        public StatusOferta Status { get; set; }
        public DateTime EnviadaEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? RespondidaEm { get; set; }
    }
}
