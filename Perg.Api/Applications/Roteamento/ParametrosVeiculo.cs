using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Roteamento
{
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

        // Tabela padrão do Perg: base + por km + por entrega. A loja pode mudar a dela
        // (Configurações > Empresas); sem mudança, vale esta.
        public static ValoresVeiculo ValoresPadrao(TipoVeiculo tipo) => tipo switch
        {
            TipoVeiculo.Bike => new ValoresVeiculo(6m, 1.00m, 1.50m),
            TipoVeiculo.Moto => new ValoresVeiculo(8m, 1.20m, 2.00m),
            TipoVeiculo.Carro => new ValoresVeiculo(15m, 1.80m, 2.50m),
            TipoVeiculo.Van => new ValoresVeiculo(30m, 2.50m, 3.00m),
            TipoVeiculo.Caminhao => new ValoresVeiculo(60m, 3.50m, 4.00m),
            _ => new ValoresVeiculo(8m, 1.20m, 2.00m)
        };

        // Valor pago ao entregador pela rota (ajustável na hora de ofertar)
        public static decimal CalcularValorEntregador(ValoresVeiculo valores, int distanciaM, int entregas)
        {
            decimal valor = valores.BaseRota + valores.PorKm * distanciaM / 1000m + valores.PorEntrega * entregas;
            return Math.Round(valor, 2);
        }

        public static decimal CalcularValorEntregador(TipoVeiculo tipo, int distanciaM, int entregas)
            => CalcularValorEntregador(ValoresPadrao(tipo), distanciaM, entregas);

        public static readonly string[] Cores =
        {
            "#E07B39", "#2E86AB", "#7CB518", "#9B5DE5", "#F15BB5", "#00A6A6", "#D62828", "#3A0CA3"
        };
    }

    // Quanto o entregador recebe: fixo por rota, por km rodado e por entrega
    public record ValoresVeiculo(decimal BaseRota, decimal PorKm, decimal PorEntrega);
}
