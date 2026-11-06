using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.OfertasDto;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Fluxo: a loja oferta a rota -> favoritos recebem primeiro -> passado o tempo da loja, a rede recebe
    // -> o primeiro que aceitar fica com a rota. Se ninguém aceitar, a rota volta para "proposta".
    public class OfertaService
    {
        private const int LimiteOfertasRede = 50;

        private readonly IOfertaRepository _repository;
        private readonly IConfiguration _config;
        private readonly NotificadorTempoReal _notificador;

        public OfertaService(IOfertaRepository repository, IConfiguration config, NotificadorTempoReal notificador)
        {
            _repository = repository;
            _config = config;
            _notificador = notificador;
        }

        private TimeSpan TempoRodadaRede => TimeSpan.FromMinutes(_config.GetValue("Ofertas:MinutosRede", 10));

        // ---------- Loja ----------

        public LerRotaDto Ofertar(Guid rotaId, OfertarRotaDto dto, Guid empresaId)
        {
            Rota rota = _repository.ObterRota(rotaId)
                ?? throw new DomainException("Rota não encontrada.");

            if (rota.Status != StatusRota.Proposta)
            {
                throw new DomainException("Só é possível ofertar rotas com status proposta.");
            }

            decimal valor = dto.ValorEntregador
                ?? rota.ValorEntregador
                ?? ParametrosVeiculo.CalcularValorEntregador(rota.TipoVeiculo, rota.DistanciaPrevistaM ?? 0, rota.Paradas.Count);

            if (valor <= 0)
            {
                throw new DomainException("O valor do entregador precisa ser maior que zero.");
            }

            Empresa empresa = _repository.ObterEmpresa(empresaId)
                ?? throw new DomainException("Empresa não encontrada.");

            DateTime agora = DateTime.UtcNow;
            string tipo = rota.TipoVeiculo.ToString().ToLowerInvariant();

            List<Guid> favoritos = _repository.ListarFavoritosDisponiveis(empresaId, rota.TipoVeiculo);

            if (favoritos.Count > 0)
            {
                CriarOfertas(rota, favoritos, RodadaOferta.Favoritos, agora.AddSeconds(empresa.TempoOfertaFavoritosS));
            }
            else if (empresa.SomenteFavoritos)
            {
                throw new DomainException($"Nenhum entregador favorito disponível com {tipo}. Desative \"Só preferenciais\" ou favorite alguém.");
            }
            else
            {
                List<Guid> rede = _repository.ListarRedeDisponivel(rota.TipoVeiculo, new List<Guid>(), LimiteOfertasRede);
                if (rede.Count == 0)
                {
                    throw new DomainException($"Nenhum entregador disponível na rede com {tipo} agora.");
                }
                CriarOfertas(rota, rede, RodadaOferta.Rede, agora.Add(TempoRodadaRede));
            }

            rota.ValorEntregador = valor;
            rota.Status = StatusRota.Ofertada;

            _repository.Adicionar(
                Array.Empty<PedidoEvento>(),
                new[] { Mensagem(rota, "RotaOfertada") },
                Array.Empty<Notificacao>());
            _repository.Salvar();

            return MapeadorRotas.ParaDto(rota);
        }

        public List<LerOfertaLojaDto> ListarDaRota(Guid rotaId)
        {
            return _repository.ListarDaRota(rotaId)
                .Select(o => new LerOfertaLojaDto
                {
                    Id = o.Id,
                    EntregadorId = o.EntregadorId,
                    EntregadorNome = o.Entregador.Nome,
                    Rodada = o.Rodada,
                    Status = o.Status,
                    EnviadaEm = o.EnviadaEm,
                    ExpiraEm = o.ExpiraEm,
                    RespondidaEm = o.RespondidaEm
                })
                .ToList();
        }

        // ---------- App do entregador ----------

        public List<LerOfertaDto> ListarDoEntregador(Guid entregadorId)
        {
            return _repository.ListarAbertasDoEntregador(entregadorId, DateTime.UtcNow)
                .Select(o =>
                {
                    Origem origem = o.Rota.Roteirizacao.Origem;
                    return new LerOfertaDto
                    {
                        Id = o.Id,
                        RotaId = o.RotaId,
                        CodigoRota = "R" + o.Rota.Numero.ToString("00"),
                        NomeLoja = o.Empresa.NomeFantasia,
                        EnderecoColeta = $"{origem.Logradouro}, {origem.Numero} - {origem.Bairro}",
                        ColetaLatitude = origem.Latitude,
                        ColetaLongitude = origem.Longitude,
                        TipoVeiculo = o.Rota.TipoVeiculo,
                        QuantidadeEntregas = o.Rota.Paradas.Count(p => p.Status != StatusParada.Cancelada),
                        Bairros = o.Rota.Paradas
                            .Select(p => p.Pedido.Bairro)
                            .Where(b => !string.IsNullOrWhiteSpace(b))
                            .Select(b => b!)
                            .Distinct()
                            .ToList(),
                        DistanciaPrevistaM = o.Rota.DistanciaPrevistaM,
                        DuracaoPrevistaS = o.Rota.DuracaoPrevistaS,
                        ColetaPrevistaEm = o.Rota.ColetaPrevistaEm,
                        ValorEntregador = o.Rota.ValorEntregador,
                        Rodada = o.Rodada,
                        ExpiraEm = o.ExpiraEm
                    };
                })
                .ToList();
        }

        public void Aceitar(Guid ofertaId, Guid entregadorId)
        {
            OfertaRota oferta = _repository.ObterParaResposta(ofertaId, entregadorId)
                ?? throw new DomainException("Oferta não encontrada.");

            DateTime agora = DateTime.UtcNow;
            Rota rota = oferta.Rota;

            if (oferta.Status != StatusOferta.Enviada || oferta.ExpiraEm <= agora || rota.Status != StatusRota.Ofertada)
            {
                throw new DomainException("Esta oferta não está mais disponível.");
            }

            Veiculo? veiculo = oferta.Entregador.VeiculoAtual;
            if (veiculo == null || veiculo.Tipo != rota.TipoVeiculo)
            {
                throw new DomainException("Seu veículo atual não é do tipo desta rota.");
            }

            oferta.Status = StatusOferta.Aceita;
            oferta.RespondidaEm = agora;

            rota.Status = StatusRota.Aceita;
            rota.EntregadorId = oferta.EntregadorId;
            rota.VeiculoId = veiculo.Id;

            foreach (OfertaRota outra in rota.OfertasRota.Where(o => o.Id != oferta.Id && o.Status == StatusOferta.Enviada))
            {
                outra.Status = StatusOferta.Cancelada;
            }

            string codigo = "R" + rota.Numero.ToString("00");
            string nome = oferta.Entregador.Nome;

            List<PedidoEvento> eventos = rota.Paradas
                .Where(p => p.Status == StatusParada.Pendente)
                .Select(p => new PedidoEvento
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = rota.EmpresaId,
                    PedidoId = p.PedidoId,
                    RotaId = rota.Id,
                    TipoEvento = "RotaAceita",
                    Descricao = $"Entregador {nome} aceitou a rota {codigo}",
                    AutorTipo = AutorEvento.Entregador,
                    AutorId = oferta.EntregadorId,
                    OcorridoEm = agora
                })
                .ToList();

            List<Notificacao> notificacoes = NotificarAdmins(rota.EmpresaId, TipoNotificacao.EntregadorAceitou,
                $"{nome} aceitou a rota {codigo}", $"/rotas/{rota.Id}");

            _repository.Adicionar(eventos, new[] { Mensagem(rota, "RotaAceita") }, notificacoes);

            try
            {
                _repository.Salvar();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Índice ux_ofertas_uma_aceita: dois aceites ao mesmo tempo, só o primeiro vale
                throw new DomainException("Outro entregador aceitou esta rota antes.");
            }

            _notificador.EnviarNotificacoes(notificacoes);
        }

        public void Recusar(Guid ofertaId, Guid entregadorId)
        {
            OfertaRota oferta = _repository.ObterParaResposta(ofertaId, entregadorId)
                ?? throw new DomainException("Oferta não encontrada.");

            if (oferta.Status != StatusOferta.Enviada)
            {
                throw new DomainException("Esta oferta não está mais disponível.");
            }

            oferta.Status = StatusOferta.Recusada;
            oferta.RespondidaEm = DateTime.UtcNow;
            _repository.Salvar();
        }

        // ---------- Segundo plano (ProcessadorOfertas, a cada 15 s) ----------

        public void ProcessarRodadas()
        {
            DateTime agora = DateTime.UtcNow;

            // 1. Ofertas vencidas
            foreach (OfertaRota vencida in _repository.ListarVencidas(agora))
            {
                vencida.Status = StatusOferta.Expirada;
            }
            _repository.Salvar();

            // 2. Rotas ofertadas que ficaram sem ninguém respondendo
            var notificacoes = new List<Notificacao>();
            var mensagens = new List<Outbox>();

            foreach (Rota rota in _repository.ListarOfertadasSemOfertaAberta())
            {
                bool redeJaRecebeu = rota.OfertasRota.Any(o => o.Rodada == RodadaOferta.Rede);
                Empresa? empresa = _repository.ObterEmpresa(rota.EmpresaId);

                if (!redeJaRecebeu && empresa != null && !empresa.SomenteFavoritos)
                {
                    List<Guid> jaReceberam = rota.OfertasRota.Select(o => o.EntregadorId).ToList();
                    List<Guid> rede = _repository.ListarRedeDisponivel(rota.TipoVeiculo, jaReceberam, LimiteOfertasRede);

                    if (rede.Count > 0)
                    {
                        CriarOfertas(rota, rede, RodadaOferta.Rede, agora.Add(TempoRodadaRede));
                        continue;
                    }
                }

                // Ninguém aceitou: a rota volta para a loja decidir (ofertar de novo, mudar valor ou veículo)
                rota.Status = StatusRota.Proposta;
                string codigo = "R" + rota.Numero.ToString("00");
                notificacoes.AddRange(NotificarAdmins(rota.EmpresaId, TipoNotificacao.Sistema,
                    $"Nenhum entregador aceitou a rota {codigo}", $"/rotas/{rota.Id}"));
                mensagens.Add(Mensagem(rota, "RotaSemEntregador"));
            }

            _repository.Adicionar(Array.Empty<PedidoEvento>(), mensagens, notificacoes);
            _repository.Salvar();
            _notificador.EnviarNotificacoes(notificacoes);
        }

        // ---------- apoio ----------

        // A tabela tem UNIQUE (rota_id, entregador_id): se o entregador já recebeu esta rota antes, reabre a oferta
        private void CriarOfertas(Rota rota, List<Guid> entregadores, RodadaOferta rodada, DateTime expiraEm)
        {
            DateTime agora = DateTime.UtcNow;
            var novas = new List<OfertaRota>();

            foreach (Guid entregadorId in entregadores)
            {
                OfertaRota? existente = rota.OfertasRota.FirstOrDefault(o => o.EntregadorId == entregadorId);
                if (existente != null)
                {
                    existente.Status = StatusOferta.Enviada;
                    existente.Rodada = rodada;
                    existente.EnviadaEm = agora;
                    existente.ExpiraEm = expiraEm;
                    existente.RespondidaEm = null;
                    continue;
                }

                var oferta = new OfertaRota
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = rota.EmpresaId,
                    RotaId = rota.Id,
                    EntregadorId = entregadorId,
                    Rodada = rodada,
                    Status = StatusOferta.Enviada,
                    EnviadaEm = agora,
                    ExpiraEm = expiraEm
                };
                rota.OfertasRota.Add(oferta);
                novas.Add(oferta);
            }

            _repository.Adicionar(novas);
        }

        private List<Notificacao> NotificarAdmins(Guid empresaId, TipoNotificacao tipo, string titulo, string link)
        {
            return _repository.ListarAdminsDaEmpresa(empresaId)
                .Select(usuarioId => new Notificacao
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = empresaId,
                    UsuarioId = usuarioId,
                    Tipo = tipo,
                    Titulo = titulo,
                    Link = link
                })
                .ToList();
        }

        private static Outbox Mensagem(Rota rota, string tipoEvento)
        {
            return new Outbox
            {
                Id = Guid.CreateVersion7(),
                Topico = "rota",
                Chave = rota.Id.ToString(),
                TipoEvento = tipoEvento,
                Payload = JsonSerializer.Serialize(new
                {
                    rotaId = rota.Id,
                    empresaId = rota.EmpresaId,
                    entregadorId = rota.EntregadorId,
                    status = rota.Status.ToString()
                })
            };
        }
    }
}
