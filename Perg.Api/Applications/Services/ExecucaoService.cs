using System.Text.Json;
using Perg.Api.Applications.Arquivos;
using Perg.Api.Applications.Execucao;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // O dia do entregador no app: jornada -> iniciar a rota na coleta -> cada parada (a caminho,
    // entregue ou ocorrência) -> a rota se conclui sozinha e o valor entra na carteira.
    public class ExecucaoService
    {
        private readonly IExecucaoRepository _repository;
        private readonly NotificadorTempoReal _notificador;

        // Notificações criadas nesta requisição, enviadas em tempo real depois de salvar
        private readonly List<Notificacao> _notificacoesNovas = new();

        public ExecucaoService(IExecucaoRepository repository, NotificadorTempoReal notificador)
        {
            _repository = repository;
            _notificador = notificador;
        }

        // ---------- Jornada ----------

        public LerJornadaDto? ObterJornada(Guid entregadorId)
        {
            Jornada? jornada = _repository.ObterJornadaAberta(entregadorId);
            return jornada == null ? null : ParaDto(jornada);
        }

        public LerJornadaDto IniciarJornada(Guid entregadorId)
        {
            if (_repository.ObterJornadaAberta(entregadorId) != null)
            {
                throw new DomainException("Você já está com a jornada iniciada.");
            }

            Entregador entregador = _repository.ObterEntregador(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.VeiculoAtual == null || !entregador.VeiculoAtual.Ativo)
            {
                throw new DomainException("Escolha um veículo ativo antes de iniciar a jornada.");
            }

            var jornada = new Jornada
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = entregadorId,
                VeiculoId = entregador.VeiculoAtualId,
                IniciadaEm = DateTime.UtcNow,
                Veiculo = entregador.VeiculoAtual
            };

            _repository.AdicionarJornada(jornada);
            return ParaDto(jornada);
        }

        public LerJornadaDto EncerrarJornada(Guid entregadorId)
        {
            Jornada jornada = _repository.ObterJornadaAberta(entregadorId)
                ?? throw new DomainException("Você não está com a jornada iniciada.");

            if (_repository.TemRotaEmAndamento(entregadorId))
            {
                throw new DomainException("Termine a rota em andamento antes de encerrar a jornada.");
            }

            jornada.EncerradaEm = DateTime.UtcNow;
            _repository.Salvar();
            return ParaDto(jornada);
        }

        // ---------- Rotas ----------

        public List<LerRotaAppDto> ListarRotas(Guid entregadorId)
        {
            return _repository.ListarRotasDoEntregador(entregadorId).Select(ParaDto).ToList();
        }

        // Na loja, com os pacotes no veículo
        public LerRotaAppDto IniciarRota(Guid rotaId, Guid entregadorId)
        {
            Rota rota = _repository.ObterRotaDoEntregador(rotaId, entregadorId)
                ?? throw new DomainException("Rota não encontrada.");

            if (rota.Status != StatusRota.Aceita)
            {
                throw new DomainException("Só é possível iniciar rotas aceitas.");
            }

            if (_repository.ObterJornadaAberta(entregadorId) == null)
            {
                throw new DomainException("Inicie a jornada antes de começar a rota.");
            }

            if (_repository.TemRotaEmAndamento(entregadorId))
            {
                throw new DomainException("Você já tem uma rota em andamento.");
            }

            DateTime agora = DateTime.UtcNow;
            rota.Status = StatusRota.EmAndamento;
            rota.IniciadaEm = agora;

            foreach (Parada parada in rota.Paradas.Where(p => p.Status == StatusParada.Pendente))
            {
                Pedido pedido = parada.Pedido;
                if (pedido.Status == StatusPedido.Roteirizado)
                {
                    pedido.Status = StatusPedido.EmRota;
                    _repository.Adicionar(Evento(pedido, rota, entregadorId, "PedidoSaiuParaEntrega",
                        "Pedido saiu para entrega", StatusPedido.EmRota));
                    _repository.Adicionar(Mensagem("pedido", pedido.Id, "PedidoSaiuParaEntrega", new { pedidoId = pedido.Id, rotaId = rota.Id }));
                }
            }

            _repository.Adicionar(Mensagem("rota", rota.Id, "RotaIniciada", new { rotaId = rota.Id, entregadorId }));
            _repository.Salvar();

            return ParaDto(rota);
        }

        // ---------- Paradas ----------

        public void MarcarACaminho(Guid paradaId, Guid entregadorId)
        {
            Parada parada = ObterParadaAtiva(paradaId, entregadorId);

            if (parada.Status != StatusParada.Pendente)
            {
                throw new DomainException("Esta entrega já está a caminho ou foi concluída.");
            }

            parada.Status = StatusParada.ACaminho;
            _repository.Adicionar(Evento(parada.Pedido, parada.Rota, entregadorId, "EntregadorACaminho",
                "Entregador a caminho do seu endereço", null));
            _repository.Salvar();
        }

        public void ConcluirEntrega(Guid paradaId, ConcluirEntregaDto dto, Guid entregadorId)
        {
            Parada parada = ObterParadaAtiva(paradaId, entregadorId);
            Pedido pedido = parada.Pedido;

            if (parada.Status != StatusParada.Pendente && parada.Status != StatusParada.ACaminho)
            {
                throw new DomainException("Esta entrega já foi concluída.");
            }

            if (!Enum.IsDefined(dto.TipoComprovante))
            {
                throw new DomainException("Tipo de comprovante inválido.");
            }

            if (dto.TipoComprovante == TipoComprovante.Pin)
            {
                if (dto.Pin?.Trim() != PinEntrega.Gerar(pedido.TokenRastreamento))
                {
                    throw new DomainException("PIN incorreto. Confira com o cliente.");
                }
            }
            else if (string.IsNullOrWhiteSpace(dto.ComprovanteChave))
            {
                throw new DomainException("Envie a foto ou a assinatura do comprovante.");
            }
            else if (!dto.ComprovanteChave.StartsWith(PastasArquivos.Comprovantes(entregadorId) + "/"))
            {
                // A chave tem que ser de um arquivo enviado por este entregador em POST api/app/arquivos
                throw new DomainException("Comprovante inválido. Envie a foto pelo app novamente.");
            }

            DateTime agora = DateTime.UtcNow;

            parada.Status = StatusParada.Entregue;
            parada.ChegadaReal ??= agora;
            parada.ConcluidaEm = agora;
            parada.ComprovanteTipo = dto.TipoComprovante;
            parada.ComprovanteChave = dto.TipoComprovante == TipoComprovante.Pin ? null : dto.ComprovanteChave;
            parada.RecebedorNome = dto.RecebedorNome?.Trim();
            parada.LatitudeConclusao = dto.Latitude;
            parada.LongitudeConclusao = dto.Longitude;

            pedido.Status = StatusPedido.Entregue;
            pedido.EntregueEm = agora;
            // O link de rastreio continua funcionando por 7 dias depois da entrega
            pedido.TokenExpiraEm = agora.AddDays(7);

            PedidoEvento evento = Evento(pedido, parada.Rota, entregadorId, "PedidoEntregue", "Pedido entregue", StatusPedido.Entregue);
            evento.Dados = JsonSerializer.Serialize(new { recebedor = parada.RecebedorNome, comprovante = dto.TipoComprovante.ToString() });
            _repository.Adicionar(evento);
            _repository.Adicionar(Mensagem("pedido", pedido.Id, "PedidoEntregue", new { pedidoId = pedido.Id, rotaId = parada.RotaId }));

            VerificarConclusao(parada.Rota, agora);
            _repository.Salvar();
            _notificador.EnviarNotificacoes(_notificacoesNovas);
        }

        public void RegistrarOcorrencia(Guid paradaId, RegistrarOcorrenciaDto dto, Guid entregadorId)
        {
            Parada parada = ObterParadaAtiva(paradaId, entregadorId);
            Pedido pedido = parada.Pedido;

            if (parada.Status != StatusParada.Pendente && parada.Status != StatusParada.ACaminho)
            {
                throw new DomainException("Esta entrega já foi concluída.");
            }

            if (!Enum.IsDefined(dto.Tipo))
            {
                throw new DomainException("Tipo de ocorrência inválido.");
            }

            if (dto.Tipo == TipoOcorrencia.Outro && string.IsNullOrWhiteSpace(dto.Descricao))
            {
                throw new DomainException("Descreva o que aconteceu.");
            }

            if (!string.IsNullOrWhiteSpace(dto.FotoChave)
                && !dto.FotoChave.StartsWith(PastasArquivos.Ocorrencias(entregadorId) + "/"))
            {
                throw new DomainException("Foto inválida. Envie a foto pelo app novamente.");
            }

            DateTime agora = DateTime.UtcNow;
            string motivo = TextosOcorrencia.Descrever(dto.Tipo);

            _repository.Adicionar(new Ocorrencia
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = parada.EmpresaId,
                ParadaId = parada.Id,
                EntregadorId = entregadorId,
                Tipo = dto.Tipo,
                Descricao = dto.Descricao,
                FotoChave = dto.FotoChave,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Status = StatusOcorrencia.Aberta,
                RegistradaEm = agora
            });

            parada.Status = StatusParada.Falhou;
            parada.ConcluidaEm = agora;
            parada.LatitudeConclusao = dto.Latitude;
            parada.LongitudeConclusao = dto.Longitude;

            pedido.Status = StatusPedido.Ocorrencia;

            _repository.Adicionar(Evento(pedido, parada.Rota, entregadorId, "OcorrenciaRegistrada",
                $"Não foi possível entregar: {motivo}", StatusPedido.Ocorrencia));
            _repository.Adicionar(Mensagem("pedido", pedido.Id, "OcorrenciaRegistrada", new { pedidoId = pedido.Id, tipo = dto.Tipo.ToString() }));

            Notificar(parada.EmpresaId, ocorrencia: true, TipoNotificacao.Ocorrencia,
                $"Pedido #{pedido.NumeroPedido}: {motivo}", $"/pedidos/{pedido.Id}");

            VerificarConclusao(parada.Rota, agora);
            _repository.Salvar();
            _notificador.EnviarNotificacoes(_notificacoesNovas);
        }

        // ---------- Carteira ----------

        public LerCarteiraDto ObterCarteira(Guid entregadorId)
        {
            return new LerCarteiraDto
            {
                Saldo = _repository.ObterSaldo(entregadorId),
                Lancamentos = _repository.ListarLancamentos(entregadorId, 50)
                    .Select(l => new LerLancamentoDto { Tipo = l.Tipo, Valor = l.Valor, Descricao = l.Descricao, CriadoEm = l.CriadoEm })
                    .ToList()
            };
        }

        // ---------- regras ----------

        private Parada ObterParadaAtiva(Guid paradaId, Guid entregadorId)
        {
            Parada parada = _repository.ObterParadaDoEntregador(paradaId, entregadorId)
                ?? throw new DomainException("Entrega não encontrada.");

            if (parada.Rota.Status != StatusRota.EmAndamento)
            {
                throw new DomainException("Inicie a rota antes de atualizar as entregas.");
            }

            return parada;
        }

        // Quando a última parada termina, a rota se conclui e o valor vai para a carteira
        private void VerificarConclusao(Rota rota, DateTime agora)
        {
            bool faltaAlguma = rota.Paradas.Any(p => p.Status == StatusParada.Pendente || p.Status == StatusParada.ACaminho);
            if (faltaAlguma)
            {
                return;
            }

            rota.Status = StatusRota.Concluida;
            rota.ConcluidaEm = agora;
            rota.Atrasada = rota.FimPrevistoEm.HasValue && agora > rota.FimPrevistoEm.Value;

            string codigo = "R" + rota.Numero.ToString("00");

            if (rota.EntregadorId.HasValue && rota.ValorEntregador is > 0m)
            {
                _repository.Adicionar(new CarteiraLancamento
                {
                    Id = Guid.CreateVersion7(),
                    EntregadorId = rota.EntregadorId.Value,
                    Tipo = TipoLancamento.Rota,
                    Valor = rota.ValorEntregador.Value,
                    RotaId = rota.Id,
                    Descricao = $"Rota {codigo} - {rota.Empresa.NomeFantasia}"
                });
            }

            int entregues = rota.Paradas.Count(p => p.Status == StatusParada.Entregue);
            int falhas = rota.Paradas.Count(p => p.Status == StatusParada.Falhou);
            Notificar(rota.EmpresaId, ocorrencia: false, TipoNotificacao.RotaConcluida,
                $"Rota {codigo} concluída: {entregues} entregue(s), {falhas} com ocorrência", $"/rotas/{rota.Id}");

            _repository.Adicionar(Mensagem("rota", rota.Id, "RotaConcluida", new { rotaId = rota.Id, entregues, falhas }));
        }

        private void Notificar(Guid empresaId, bool ocorrencia, TipoNotificacao tipo, string titulo, string link)
        {
            foreach (Guid usuarioId in _repository.ListarUsuariosParaNotificar(empresaId, ocorrencia))
            {
                var notificacao = new Notificacao
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = empresaId,
                    UsuarioId = usuarioId,
                    Tipo = tipo,
                    Titulo = titulo,
                    Link = link
                };
                _repository.Adicionar(notificacao);
                _notificacoesNovas.Add(notificacao);
            }
        }

        private static PedidoEvento Evento(Pedido pedido, Rota rota, Guid entregadorId, string tipo, string descricao, StatusPedido? statusNovo)
        {
            return new PedidoEvento
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = pedido.EmpresaId,
                PedidoId = pedido.Id,
                RotaId = rota.Id,
                TipoEvento = tipo,
                StatusNovo = statusNovo,
                Descricao = descricao,
                AutorTipo = AutorEvento.Entregador,
                AutorId = entregadorId,
                OcorridoEm = DateTime.UtcNow
            };
        }

        private static Outbox Mensagem(string topico, Guid chave, string tipoEvento, object payload)
        {
            return new Outbox
            {
                Id = Guid.CreateVersion7(),
                Topico = topico,
                Chave = chave.ToString(),
                TipoEvento = tipoEvento,
                Payload = JsonSerializer.Serialize(payload)
            };
        }

        private static LerJornadaDto ParaDto(Jornada jornada)
        {
            return new LerJornadaDto
            {
                Id = jornada.Id,
                IniciadaEm = jornada.IniciadaEm,
                EncerradaEm = jornada.EncerradaEm,
                VeiculoTipo = jornada.Veiculo?.Tipo
            };
        }

        private static LerRotaAppDto ParaDto(Rota rota)
        {
            Origem origem = rota.Roteirizacao.Origem;
            return new LerRotaAppDto
            {
                Id = rota.Id,
                Codigo = "R" + rota.Numero.ToString("00"),
                Status = rota.Status,
                NomeLoja = rota.Empresa.NomeFantasia,
                EnderecoColeta = $"{origem.Logradouro}, {origem.Numero} - {origem.Bairro}, {origem.Cidade}",
                ColetaLatitude = origem.Latitude,
                ColetaLongitude = origem.Longitude,
                ColetaPrevistaEm = rota.ColetaPrevistaEm,
                ValorEntregador = rota.ValorEntregador,
                DistanciaPrevistaM = rota.DistanciaPrevistaM,
                DuracaoPrevistaS = rota.DuracaoPrevistaS,
                Trajeto = rota.Trajeto,
                Paradas = rota.Paradas
                    .Where(p => p.Status != StatusParada.Cancelada)
                    .OrderBy(p => p.Sequencia)
                    .Select(p => new LerParadaAppDto
                    {
                        Id = p.Id,
                        Sequencia = p.Sequencia,
                        Status = p.Status,
                        ChegadaPrevista = p.ChegadaPrevista,
                        NumeroPedido = p.Pedido.NumeroPedido,
                        ClienteNome = p.Pedido.ClienteNome,
                        ClienteTelefone = p.Pedido.ClienteTelefone,
                        Endereco = $"{p.Pedido.Logradouro}, {p.Pedido.Numero} - {p.Pedido.Bairro}, {p.Pedido.Cidade}",
                        Complemento = p.Pedido.Complemento,
                        Referencia = p.Pedido.Referencia,
                        Latitude = p.Pedido.Latitude,
                        Longitude = p.Pedido.Longitude,
                        Volumes = p.Pedido.Volumes,
                        Observacoes = p.Pedido.Observacoes,
                        JanelaInicio = p.Pedido.JanelaInicio,
                        JanelaFim = p.Pedido.JanelaFim
                    })
                    .ToList()
            };
        }
    }
}
