using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.OcorrenciasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;
using Perg.Api.Applications.Eventos;

namespace Perg.Api.Applications.Services
{
    public class OcorrenciaService
    {
        private readonly IOcorrenciaRepository _repository;

        public OcorrenciaService(IOcorrenciaRepository repository)
        {
            _repository = repository;
        }

        public List<LerOcorrenciaDto> Listar(bool somenteAbertas)
        {
            return _repository.Listar(somenteAbertas).Select(ParaDto).ToList();
        }

        public LerOcorrenciaDto Resolver(Guid id, ResolverOcorrenciaDto dto, Guid usuarioId)
        {
            Ocorrencia ocorrencia = _repository.ObterPorId(id)
                ?? throw new DomainException("Ocorrência não encontrada.");

            if (ocorrencia.Status != StatusOcorrencia.Aberta)
            {
                throw new DomainException("Esta ocorrência já foi resolvida.");
            }

            if (!Enum.IsDefined(dto.Acao))
            {
                throw new DomainException("Ação inválida.");
            }

            DateTime agora = DateTime.UtcNow;
            ocorrencia.Status = StatusOcorrencia.Resolvida;
            ocorrencia.ResolvidaPor = usuarioId;
            ocorrencia.ResolvidaEm = agora;

            Pedido pedido = ocorrencia.Parada.Pedido;
            if (pedido.Status == StatusPedido.Ocorrencia)
            {
                bool reentregar = dto.Acao == AcaoOcorrencia.Reentregar;
                pedido.Status = reentregar ? StatusPedido.Novo : StatusPedido.Cancelado;

                string tipoEvento = reentregar ? "PedidoReagendado" : "PedidoCancelado";
                _repository.Adicionar(FabricaEventos.LinhaDoTempo(pedido, tipoEvento,
                    reentregar ? "Nova tentativa de entrega agendada" : "Pedido cancelado",
                    pedido.Status, AutorEvento.Usuario, usuarioId,
                    dados: new { ocorrenciaId = ocorrencia.Id, observacao = dto.Observacao }));

                _repository.Adicionar(FabricaEventos.DePedido(pedido, tipoEvento, new { ocorrenciaId = ocorrencia.Id }));
            }

            _repository.Salvar();
            return ParaDto(ocorrencia);
        }

        private static LerOcorrenciaDto ParaDto(Ocorrencia o)
        {
            return new LerOcorrenciaDto
            {
                Id = o.Id,
                Tipo = o.Tipo,
                Status = o.Status,
                Descricao = o.Descricao,
                FotoChave = o.FotoChave,
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                RegistradaEm = o.RegistradaEm,
                ResolvidaEm = o.ResolvidaEm,
                PedidoId = o.Parada.PedidoId,
                NumeroPedido = o.Parada.Pedido.NumeroPedido,
                ClienteNome = o.Parada.Pedido.ClienteNome,
                RotaId = o.Parada.RotaId,
                EntregadorNome = o.Entregador?.Nome
            };
        }
    }
}
