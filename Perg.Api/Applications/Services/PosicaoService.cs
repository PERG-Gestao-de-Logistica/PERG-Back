using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.PosicoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class PosicaoService
    {
        private const int MaximoPorLote = 100;

        private readonly IPosicaoRepository _repository;
        private readonly IPosicaoAtualCache _cache;
        private readonly NotificadorTempoReal _notificador;

        public PosicaoService(IPosicaoRepository repository, IPosicaoAtualCache cache, NotificadorTempoReal notificador)
        {
            _repository = repository;
            _cache = cache;
            _notificador = notificador;
        }

        // App do entregador
        public int Registrar(Guid entregadorId, EnviarPosicoesDto dto)
        {
            if (dto.Posicoes == null || dto.Posicoes.Count == 0)
            {
                throw new DomainException("Nenhuma posição enviada.");
            }

            if (dto.Posicoes.Count > MaximoPorLote)
            {
                throw new DomainException($"Envie no máximo {MaximoPorLote} posições por vez.");
            }

            if (!_repository.TemJornadaAberta(entregadorId))
            {
                throw new DomainException("Inicie a jornada para compartilhar sua localização.");
            }

            DateTime agora = DateTime.UtcNow;

            // Descarta leituras impossíveis: fora do mapa, do futuro ou com mais de 1 dia
            List<PosicaoDto> validas = dto.Posicoes
                .Where(p => p.Latitude is >= -90 and <= 90 && p.Longitude is >= -180 and <= 180)
                .Where(p => p.RegistradaEm.UtcDateTime <= agora.AddMinutes(1) && p.RegistradaEm.UtcDateTime >= agora.AddDays(-1))
                .ToList();

            if (validas.Count == 0)
            {
                return 0;
            }

            Rota? rota = _repository.ObterRotaEmAndamento(entregadorId);

            List<PosicaoEntregador> posicoes = validas.Select(p => new PosicaoEntregador
            {
                EntregadorId = entregadorId,
                RegistradaEm = TruncarMilissegundos(p.RegistradaEm.UtcDateTime),
                EmpresaId = rota?.EmpresaId,
                RotaId = rota?.Id,
                Latitude = Math.Round(p.Latitude, 6),
                Longitude = Math.Round(p.Longitude, 6),
                PrecisaoM = p.PrecisaoM.HasValue ? Math.Round(p.PrecisaoM.Value, 1) : null,
                VelocidadeKmh = p.VelocidadeKmh.HasValue ? Math.Round(p.VelocidadeKmh.Value, 1) : null
            }).ToList();

            _repository.Inserir(posicoes);

            // A mais recente vira a posição atual e vai em tempo real para quem está olhando
            PosicaoEntregador ultima = posicoes.OrderByDescending(p => p.RegistradaEm).First();
            Entregador? entregador = _repository.ObterEntregador(entregadorId);

            var atual = new PosicaoAtual
            {
                EntregadorId = entregadorId,
                EntregadorNome = entregador?.Nome ?? string.Empty,
                EmpresaId = rota?.EmpresaId,
                RotaId = rota?.Id,
                Latitude = ultima.Latitude,
                Longitude = ultima.Longitude,
                VelocidadeKmh = ultima.VelocidadeKmh,
                RegistradaEm = ultima.RegistradaEm
            };
            _cache.Atualizar(atual);

            if (rota != null)
            {
                _notificador.EnviarPosicaoParaLoja(rota.EmpresaId, atual);

                // Só o cliente da próxima entrega vê o entregador se aproximando
                Parada? proxima = rota.Paradas
                    .Where(p => p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho)
                    .OrderBy(p => p.Sequencia)
                    .FirstOrDefault();

                if (proxima != null)
                {
                    _notificador.EnviarPosicaoParaCliente(proxima.PedidoId, atual);
                }
            }

            return posicoes.Count;
        }

        // Mapa da loja: quem está em rota agora
        public List<LerPosicaoAtualDto> ListarAtuais(Guid empresaId)
        {
            return _cache.ListarDaEmpresa(empresaId)
                .Select(p => new LerPosicaoAtualDto
                {
                    EntregadorId = p.EntregadorId,
                    EntregadorNome = p.EntregadorNome,
                    RotaId = p.RotaId,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    VelocidadeKmh = p.VelocidadeKmh,
                    RegistradaEm = p.RegistradaEm
                })
                .ToList();
        }

        public List<LerPontoTrilhaDto> ListarTrilha(Guid rotaId)
        {
            return _repository.ListarTrilha(rotaId)
                .Select(p => new LerPontoTrilhaDto { Latitude = p.Latitude, Longitude = p.Longitude, RegistradaEm = p.RegistradaEm })
                .ToList();
        }

        // A chave da tabela é (entregador, registrada_em): mesma leitura reenviada = mesma chave
        private static DateTime TruncarMilissegundos(DateTime data)
        {
            return new DateTime(data.Ticks - data.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }
    }
}
