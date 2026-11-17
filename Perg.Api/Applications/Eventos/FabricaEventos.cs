using System.Text.Json;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Eventos
{
    // Formato único dos eventos. Payload: id, empresa, status em snake_case, data e, em "dados", o que for
    // específico. Chave = id do pedido/rota (mantém a ordem).
    public static class FabricaEventos
    {
        public static Outbox DePedido(Pedido pedido, string tipoEvento, object? dados = null)
        {
            return Criar("pedido", pedido.Id, tipoEvento, new
            {
                pedidoId = pedido.Id,
                empresaId = pedido.EmpresaId,
                status = ConversorEnumSnakeCase<StatusPedido>.ParaTexto(pedido.Status),
                ocorridoEm = DateTime.UtcNow,
                dados
            });
        }

        public static Outbox DeRota(Rota rota, string tipoEvento, object? dados = null)
        {
            return Criar("rota", rota.Id, tipoEvento, new
            {
                rotaId = rota.Id,
                empresaId = rota.EmpresaId,
                entregadorId = rota.EntregadorId,
                status = ConversorEnumSnakeCase<StatusRota>.ParaTexto(rota.Status),
                ocorridoEm = DateTime.UtcNow,
                dados
            });
        }

        public static Outbox DeRoteirizacao(Perg.Api.Domains.Roteirizacao roteirizacao, string tipoEvento, object? dados = null)
        {
            return Criar("roteirizacao", roteirizacao.Id, tipoEvento, new
            {
                roteirizacaoId = roteirizacao.Id,
                empresaId = roteirizacao.EmpresaId,
                ocorridoEm = DateTime.UtcNow,
                dados
            });
        }

        public static PedidoEvento LinhaDoTempo(Pedido pedido, string tipoEvento, string descricao, StatusPedido? statusNovo,
            AutorEvento autor, Guid? autorId = null, Guid? rotaId = null, object? dados = null)
        {
            return new PedidoEvento
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = pedido.EmpresaId,
                PedidoId = pedido.Id,
                RotaId = rotaId,
                TipoEvento = tipoEvento,
                StatusNovo = statusNovo,
                Descricao = descricao,
                AutorTipo = autor,
                AutorId = autorId,
                Dados = dados == null ? null : JsonSerializer.Serialize(dados),
                OcorridoEm = DateTime.UtcNow
            };
        }

        private static Outbox Criar(string topico, Guid chave, string tipoEvento, object payload)
        {
            return new Outbox
            {
                Id = Guid.CreateVersion7(),
                Topico = topico,
                Chave = chave.ToString(),
                TipoEvento = tipoEvento,
                Payload = JsonSerializer.Serialize(payload),
                CriadoEm = DateTime.UtcNow
            };
        }
    }
}
