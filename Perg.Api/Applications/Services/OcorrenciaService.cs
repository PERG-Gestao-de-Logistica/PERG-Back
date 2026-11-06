using System.Text.Json;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.OcorrenciasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // A loja decide o que fazer com o pedido que voltou com ocorrência
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
                _repository.Adicionar(new PedidoEvento
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = pedido.EmpresaId,
                    PedidoId = pedido.Id,
                    TipoEvento = tipoEvento,
                    StatusNovo = pedido.Status,
                    Descricao = reentregar ? "Nova tentativa de entrega agendada" : "Pedido cancelado",
                    AutorTipo = AutorEvento.Usuario,
                    AutorId = usuarioId,
                    Dados = JsonSerializer.Serialize(new { ocorrenciaId = ocorrencia.Id, observacao = dto.Observacao }),
                    OcorridoEm = agora
                });

                _repository.Adicionar(new Outbox
                {
                    Id = Guid.CreateVersion7(),
                    Topico = "pedido",
                    Chave = pedido.Id.ToString(),
                    TipoEvento = tipoEvento,
                    Payload = JsonSerializer.Serialize(new { pedidoId = pedido.Id, ocorrenciaId = ocorrencia.Id })
                });
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
