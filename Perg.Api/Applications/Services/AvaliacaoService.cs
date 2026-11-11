using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.AvaliacoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // A loja avalia o entregador por rota concluída (1 a 5, comentário opcional), uma única vez.
    // A média e o total entram na rede de entregadores (vw_entregadores_rede) e aparecem no app do entregador.
    public class AvaliacaoService
    {
        // Rotas concluídas há mais tempo que isso não aparecem mais para avaliar
        private const int DiasParaAvaliar = 30;

        private readonly IAvaliacaoRepository _repository;

        public AvaliacaoService(IAvaliacaoRepository repository)
        {
            _repository = repository;
        }

        public List<LerRotaParaAvaliarDto> ListarPendentes()
        {
            return _repository.ListarRotasSemAvaliacao(DateTime.UtcNow.AddDays(-DiasParaAvaliar), 50)
                .Select(r => new LerRotaParaAvaliarDto
                {
                    RotaId = r.Id,
                    Codigo = Codigo(r),
                    DataOperacao = r.DataOperacao,
                    ConcluidaEm = r.ConcluidaEm,
                    EntregadorId = r.EntregadorId!.Value,
                    EntregadorNome = r.Entregador?.Nome ?? string.Empty,
                    Entregas = r.Paradas.Count(p => p.Status == StatusParada.Entregue)
                })
                .ToList();
        }

        public LerAvaliacaoDto Avaliar(AvaliarRotaDto dto, Guid empresaId, Guid usuarioId)
        {
            if (dto.Nota < 1 || dto.Nota > 5)
            {
                throw new DomainException("A nota vai de 1 a 5.");
            }

            string? comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim();
            if (comentario?.Length > 500)
            {
                throw new DomainException("Comentário muito longo (máximo 500 caracteres).");
            }

            // Filtro por empresa: rota de outra loja não é encontrada
            Rota rota = _repository.ObterRotaComParadas(dto.RotaId) ?? throw new DomainException("Rota não encontrada.");

            if (rota.Status != StatusRota.Concluida || rota.EntregadorId == null)
            {
                throw new DomainException("Só dá para avaliar rotas concluídas.");
            }

            if (rota.ConcluidaEm < DateTime.UtcNow.AddDays(-DiasParaAvaliar))
            {
                throw new DomainException($"O prazo para avaliar esta rota ({DiasParaAvaliar} dias) já passou.");
            }

            if (_repository.RotaJaAvaliada(rota.Id))
            {
                throw new DomainException("Esta rota já foi avaliada.");
            }

            var avaliacao = new Avaliacao
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                EntregadorId = rota.EntregadorId.Value,
                RotaId = rota.Id,
                Nota = dto.Nota,
                Comentario = comentario,
                AvaliadoPor = usuarioId,
                CriadoEm = DateTime.UtcNow
            };
            _repository.Adicionar(avaliacao);

            return new LerAvaliacaoDto
            {
                Id = avaliacao.Id,
                RotaId = rota.Id,
                CodigoRota = Codigo(rota),
                DataOperacao = rota.DataOperacao,
                Nota = avaliacao.Nota,
                Comentario = avaliacao.Comentario,
                EntregadorNome = rota.Entregador?.Nome,
                CriadoEm = avaliacao.CriadoEm
            };
        }

        public List<LerAvaliacaoDto> ListarDaEmpresa()
        {
            return _repository.ListarDaEmpresa(100)
                .Select(a => new LerAvaliacaoDto
                {
                    Id = a.Id,
                    RotaId = a.RotaId,
                    CodigoRota = Codigo(a.Rota),
                    DataOperacao = a.Rota.DataOperacao,
                    Nota = a.Nota,
                    Comentario = a.Comentario,
                    EntregadorNome = a.Entregador.Nome,
                    CriadoEm = a.CriadoEm
                })
                .ToList();
        }

        public LerMinhasAvaliacoesDto MinhasAvaliacoes(Guid entregadorId)
        {
            List<Avaliacao> todas = _repository.ListarDoEntregador(entregadorId);

            return new LerMinhasAvaliacoesDto
            {
                Total = todas.Count,
                NotaMedia = todas.Count == 0 ? null : Math.Round((decimal)todas.Average(a => a.Nota), 2),
                PorNota = Enumerable.Range(1, 5).ToDictionary(n => n, n => todas.Count(a => a.Nota == n)),
                Ultimas = todas.Take(20).Select(a => new LerAvaliacaoDto
                {
                    Id = a.Id,
                    RotaId = a.RotaId,
                    CodigoRota = Codigo(a.Rota),
                    DataOperacao = a.Rota.DataOperacao,
                    Nota = a.Nota,
                    Comentario = a.Comentario,
                    NomeLoja = a.Empresa.NomeFantasia,
                    CriadoEm = a.CriadoEm
                }).ToList()
            };
        }

        private static string Codigo(Rota r) => "R" + r.Numero.ToString("00");
    }
}
