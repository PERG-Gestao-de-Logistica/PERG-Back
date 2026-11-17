using Perg.Api.Applications.Roteamento;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;
using Perg.Api.Applications.Comum;
using Perg.Api.Applications.Eventos;

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

            foreach (OfertaRota oferta in rota.OfertasRota.Where(o => o.Status == StatusOferta.Enviada))
            {
                oferta.Status = StatusOferta.Cancelada;
            }
            string codigo = Formatos.CodigoRota(rota);

            var eventos = new List<PedidoEvento>();
            var mensagens = new List<Outbox>();

            foreach (Parada parada in rota.Paradas.Where(p => p.Status == StatusParada.Pendente))
            {
                parada.Status = StatusParada.Cancelada;

                Pedido pedido = parada.Pedido;
                if (pedido.Status == StatusPedido.Roteirizado)
                {
                    pedido.Status = StatusPedido.Novo;

                    eventos.Add(FabricaEventos.LinhaDoTempo(pedido, "PedidoRemovidoDaRota", $"Pedido retirado da rota {codigo}",
                        StatusPedido.Novo, AutorEvento.Usuario, usuarioId, rota.Id));
                }
            }

            mensagens.Add(FabricaEventos.DeRota(rota, "RotaCancelada"));

            _repository.Atualizar(eventos, mensagens);

            return MapeadorRotas.ParaDto(rota);
        }
    }
}
