using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.RoteirizacoesDto
{
    public class CriarRoteirizacaoDto
    {
        // Se não vier, usa a origem padrão
        public Guid? OrigemId { get; set; }

        public DateOnly DataOperacao { get; set; }

        // Se não vier, pega todos os pedidos prontos da origem (status novo + localização ok)
        public List<Guid>? PedidoIds { get; set; }

        // Ex.: [{ "tipo": "Moto", "quantidade": 3 }, { "tipo": "Carro", "quantidade": 1 }]
        public List<FrotaDto> Frota { get; set; } = new();

        public ObjetivoRoteirizacao Objetivo { get; set; } = ObjetivoRoteirizacao.Custo;

        // Duração máxima de cada rota (ida e volta)
        public decimal JornadaHoras { get; set; } = 8;

        // Tempo parado em cada entrega
        public int TempoParadaMinutos { get; set; } = 5;

        // Se o veículo ficaria esperando mais que isso entre duas entregas, a rota vira duas viagens
        public int EsperaMaximaMinutos { get; set; } = 60;
    }

    public class FrotaDto
    {
        public TipoVeiculo Tipo { get; set; }
        public int Quantidade { get; set; }

        // Opcionais: se não vierem, usa o padrão do tipo (moto 25 kg / 6 volumes, carro 300 kg / 40...)
        public decimal? CapacidadeKg { get; set; }
        public int? CapacidadeVolumes { get; set; }
    }
}
