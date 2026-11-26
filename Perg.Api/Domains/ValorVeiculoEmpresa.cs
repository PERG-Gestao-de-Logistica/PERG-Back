using Perg.Api.Domains.Enums;

namespace Perg.Api.Domains;

// Valor que a loja paga ao entregador por tipo de veículo. Sem linha para o tipo = tabela padrão do Perg.
public class ValorVeiculoEmpresa
{
    public Guid EmpresaId { get; set; }

    public TipoVeiculo TipoVeiculo { get; set; }

    public decimal BaseRota { get; set; }

    public decimal PorKm { get; set; }

    public decimal PorEntrega { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
