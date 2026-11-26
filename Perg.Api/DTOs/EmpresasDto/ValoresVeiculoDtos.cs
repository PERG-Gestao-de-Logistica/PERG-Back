using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.EmpresasDto
{
    // GET /api/Empresas/eu/valores-entregador: um item por tipo de veículo
    public class LerValorVeiculoDto
    {
        public TipoVeiculo Tipo { get; set; }
        public decimal BaseRota { get; set; }
        public decimal PorKm { get; set; }
        public decimal PorEntrega { get; set; }

        // true = a loja mudou algum valor desse tipo
        public bool Personalizado { get; set; }

        // Tabela padrão do Perg, para a tela mostrar e restaurar
        public decimal PadraoBaseRota { get; set; }
        public decimal PadraoPorKm { get; set; }
        public decimal PadraoPorEntrega { get; set; }
    }

    // PUT /api/Empresas/eu/valores-entregador (só admin). Tipos que não vierem voltam para o padrão.
    public class AtualizarValoresVeiculoDto
    {
        public List<ValorVeiculoDto> Valores { get; set; } = new();
    }

    public class ValorVeiculoDto
    {
        public TipoVeiculo Tipo { get; set; }
        public decimal BaseRota { get; set; }
        public decimal PorKm { get; set; }
        public decimal PorEntrega { get; set; }
    }
}
