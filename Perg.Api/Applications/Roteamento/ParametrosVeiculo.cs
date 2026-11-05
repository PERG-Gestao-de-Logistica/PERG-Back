using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Roteamento
{
    // Capacidade padrão por tipo de veículo, usada quando a loja não informa outra
    public static class ParametrosVeiculo
    {
        public static decimal CapacidadeKg(TipoVeiculo tipo) => tipo switch
        {
            TipoVeiculo.Bike => 8m,
            TipoVeiculo.Moto => 25m,
            TipoVeiculo.Carro => 300m,
            TipoVeiculo.Van => 1200m,
            TipoVeiculo.Caminhao => 5000m,
            _ => 25m
        };

        public static int CapacidadeVolumes(TipoVeiculo tipo) => tipo switch
        {
            TipoVeiculo.Bike => 4,
            TipoVeiculo.Moto => 6,
            TipoVeiculo.Carro => 40,
            TipoVeiculo.Van => 150,
            TipoVeiculo.Caminhao => 500,
            _ => 6
        };

        // Valor pago ao entregador: base + por km + por entrega (tabela inicial, ajustável na hora de ofertar)
        public static decimal CalcularValorEntregador(TipoVeiculo tipo, int distanciaM, int entregas)
        {
            (decimal baseRota, decimal porKm, decimal porEntrega) = tipo switch
            {
                TipoVeiculo.Bike => (6m, 1.00m, 1.50m),
                TipoVeiculo.Moto => (8m, 1.20m, 2.00m),
                TipoVeiculo.Carro => (15m, 1.80m, 2.50m),
                TipoVeiculo.Van => (30m, 2.50m, 3.00m),
                TipoVeiculo.Caminhao => (60m, 3.50m, 4.00m),
                _ => (8m, 1.20m, 2.00m)
            };

            decimal valor = baseRota + porKm * distanciaM / 1000m + porEntrega * entregas;
            return Math.Round(valor, 2);
        }

        // Cores das rotas no mapa (R01, R02...)
        public static readonly string[] Cores =
        {
            "#E07B39", "#2E86AB", "#7CB518", "#9B5DE5", "#F15BB5", "#00A6A6", "#D62828", "#3A0CA3"
        };
    }
}
