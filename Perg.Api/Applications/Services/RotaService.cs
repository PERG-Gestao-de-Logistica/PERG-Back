using System.Text.Json;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class RotaService
    {
        private readonly IRotaRepository _repository;

        public RotaService(IRotaRepository repository)
        {
            _repository = repository;
        }

        public List<LerRotaDto> ListarPorData(DateOnly data)
        {
            return _repository.ListarPorData(data).Select(MapeadorRotas.ParaDto).ToList();
        }

        public LerRotaDto ObterPorId(Guid id)
        {
            Rota rota = _repository.ObterPorId(id)
                ?? throw new DomainException("Rota não encontrada.");

            return MapeadorRotas.ParaDto(rota);
        }

        // Desfaz uma rota que ainda não foi aceita: os pedidos voltam para "novo"
        public LerRotaDto Cancelar(Guid id, Guid usuarioId)
        {
            Rota rota = _repository.ObterPorId(id)
                ?? throw new DomainException("Rota não encontrada.");

            if (rota.Status != StatusRota.Proposta && rota.Status != StatusRota.Ofertada)
            {
                throw new DomainException("Só é possível cancelar rotas que ainda não foram aceitas por um entregador.");
            }

            rota.Status = StatusRota.Cancelada;

            // Ofertas em aberto deixam de valer
            foreach (OfertaRota oferta in rota.OfertasRota.Where(o => o.Status == StatusOferta.Enviada))
            {
                oferta.Status = StatusOferta.Cancelada;
            }
            string codigo = "R" + rota.Numero.ToString("00");

            var eventos = new List<PedidoEvento>();
            var mensagens = new List<Outbox>();

            foreach (Parada parada in rota.Paradas.Where(p => p.Status == StatusParada.Pendente))
            {
                parada.Status = StatusParada.Cancelada;

                Pedido pedido = parada.Pedido;
                if (pedido.Status == StatusPedido.Roteirizado)
                {
                    pedido.Status = StatusPedido.Novo;

                    eventos.Add(new PedidoEvento
                    {
                        Id = Guid.CreateVersion7(),
                        EmpresaId = pedido.EmpresaId,
                        PedidoId = pedido.Id,
                        RotaId = rota.Id,
                        TipoEvento = "PedidoRemovidoDaRota",
                        StatusNovo = StatusPedido.Novo,
                        Descricao = $"Pedido retirado da rota {codigo}",
                        AutorTipo = AutorEvento.Usuario,
                        AutorId = usuarioId,
                        OcorridoEm = DateTime.UtcNow
                    });
                }
            }

            mensagens.Add(new Outbox
            {
                Id = Guid.CreateVersion7(),
                Topico = "rota",
                Chave = rota.Id.ToString(),
                TipoEvento = "RotaCancelada",
                Payload = JsonSerializer.Serialize(new { rotaId = rota.Id, empresaId = rota.EmpresaId })
            });

            _repository.Atualizar(eventos, mensagens);

            return MapeadorRotas.ParaDto(rota);
        }
    }
}
