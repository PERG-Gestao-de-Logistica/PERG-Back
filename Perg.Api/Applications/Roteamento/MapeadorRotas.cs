using Perg.Api.Domains;
using Perg.Api.DTOs.RotasDto;

namespace Perg.Api.Applications.Roteamento
{
    public static class MapeadorRotas
    {
        public static LerRotaDto ParaDto(Rota rota)
        {
            return new LerRotaDto
            {
                Id = rota.Id,
                Codigo = "R" + rota.Numero.ToString("00"),
                Numero = rota.Numero,
                Regiao = rota.Regiao,
                TipoVeiculo = rota.TipoVeiculo,
                Status = rota.Status,
                Cor = rota.Cor,
                DataOperacao = rota.DataOperacao,
                DistanciaPrevistaM = rota.DistanciaPrevistaM,
                DuracaoPrevistaS = rota.DuracaoPrevistaS,
                ColetaPrevistaEm = rota.ColetaPrevistaEm,
                FimPrevistoEm = rota.FimPrevistoEm,
                Trajeto = rota.Trajeto,
                ValorEntregador = rota.ValorEntregador,
                EntregadorId = rota.EntregadorId,
                EntregadorNome = rota.Entregador?.Nome,
                Paradas = rota.Paradas
                    .OrderBy(p => p.Sequencia)
                    .Select(p => new LerParadaDto
                    {
                        Id = p.Id,
                        Sequencia = p.Sequencia,
                        Status = p.Status,
                        ChegadaPrevista = p.ChegadaPrevista,
                        PedidoId = p.PedidoId,
                        NumeroPedido = p.Pedido.NumeroPedido,
                        ClienteNome = p.Pedido.ClienteNome,
                        Logradouro = p.Pedido.Logradouro,
                        Numero = p.Pedido.Numero,
                        Bairro = p.Pedido.Bairro,
                        Latitude = p.Pedido.Latitude,
                        Longitude = p.Pedido.Longitude,
                        Volumes = p.Pedido.Volumes,
                        JanelaInicio = p.Pedido.JanelaInicio,
                        JanelaFim = p.Pedido.JanelaFim
                    }).ToList()
            };
        }
    }
}
