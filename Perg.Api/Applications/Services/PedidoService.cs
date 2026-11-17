using System.Security.Cryptography;
using System.Text.Json;
using Perg.Api.Applications.Assinaturas;
using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs;
using Perg.Api.DTOs.PedidosDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;
using Perg.Api.Applications.Eventos;

namespace Perg.Api.Applications.Services
{
    public class PedidoService
    {
        private readonly IPedidoRepository _repository;
        private readonly IOrigemRepository _origemRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IGeocodificador _geocodificador;
        private readonly LimitesPlano _limitesPlano;

        public PedidoService(IPedidoRepository repository, IOrigemRepository origemRepository,
            IEmpresaRepository empresaRepository, IGeocodificador geocodificador, LimitesPlano limitesPlano)
        {
            _repository = repository;
            _origemRepository = origemRepository;
            _empresaRepository = empresaRepository;
            _geocodificador = geocodificador;
            _limitesPlano = limitesPlano;
        }

        public ListaPaginadaDto<LerPedidoResumoDto> Listar(FiltroPedidosDto filtro, Guid empresaId)
        {
            int pagina = Math.Max(filtro.Pagina, 1);
            int tamanhoPagina = Math.Clamp(filtro.TamanhoPagina, 1, 100);

            // As datas do filtro estão no fuso da loja; o banco guarda em UTC
            Empresa empresa = _empresaRepository.ObterPorId(empresaId)
                ?? throw new DomainException("Empresa não encontrada.");
            TimeZoneInfo fuso = TimeZoneInfo.FindSystemTimeZoneById(empresa.FusoHorario);

            DateTime? criadoDesde = null;
            if (filtro.DataInicio.HasValue)
            {
                criadoDesde = TimeZoneInfo.ConvertTimeToUtc(filtro.DataInicio.Value.ToDateTime(TimeOnly.MinValue), fuso);
            }

            DateTime? criadoAte = null;
            if (filtro.DataFim.HasValue)
            {
                // Até o fim do dia: usa o início do dia seguinte como limite (exclusivo)
                criadoAte = TimeZoneInfo.ConvertTimeToUtc(filtro.DataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), fuso);
            }

            var (itens, total) = _repository.Listar(filtro.Status, criadoDesde, criadoAte, filtro.Busca, pagina, tamanhoPagina);

            return new ListaPaginadaDto<LerPedidoResumoDto>
            {
                Itens = itens.Select(p => new LerPedidoResumoDto
                {
                    Id = p.Id,
                    NumeroPedido = p.NumeroPedido,
                    NumeroExterno = p.NumeroExterno,
                    Status = p.Status,
                    Modalidade = p.Modalidade,
                    ClienteNome = p.ClienteNome,
                    Bairro = p.Bairro,
                    Cidade = p.Cidade,
                    JanelaInicio = p.JanelaInicio,
                    JanelaFim = p.JanelaFim,
                    Volumes = p.Volumes,
                    GeocodificacaoStatus = p.GeocodificacaoStatus,
                    CriadoEm = p.CriadoEm
                }).ToList(),
                Total = total,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };
        }

        public LerPedidoDto ObterPorId(Guid id)
        {
            Pedido pedido = _repository.ObterPorId(id)
                ?? throw new DomainException("Pedido não encontrado.");

            return ParaDto(pedido);
        }

        public LerPedidoDto Criar(CriarPedidoDto dto, Guid empresaId, Guid usuarioId)
        {
            Validar(dto);
            _limitesPlano.ConferirNovoPedido(empresaId);

            Pedido pedido = NovoPedido(dto, empresaId, CanalPedido.Manual, idExterno: null);

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoCriado", "Pedido criado", StatusPedido.Novo, AutorEvento.Usuario, usuarioId);
            Outbox mensagem = FabricaEventos.DePedido(pedido, "PedidoCriado");

            _repository.Adicionar(pedido, evento, mensagem);

            // Recarrega para trazer o número gerado pelo banco, a origem e a linha do tempo
            return ObterPorId(pedido.Id);
        }

        public LerPedidoDto Atualizar(Guid id, AtualizarPedidoDto dto, Guid usuarioId)
        {
            Pedido pedido = _repository.ObterPorId(id)
                ?? throw new DomainException("Pedido não encontrado.");

            if (pedido.Status != StatusPedido.Novo)
            {
                throw new DomainException("Só é possível editar pedidos com status novo.");
            }

            Validar(dto);

            Origem origem = ObterOrigem(dto.OrigemId ?? pedido.OrigemId);

            bool enderecoMudou = pedido.Cep != SomenteDigitos(dto.Cep)
                || pedido.Logradouro != dto.Logradouro?.Trim()
                || pedido.Numero != dto.Numero?.Trim()
                || pedido.Cidade != dto.Cidade?.Trim()
                || pedido.Uf != dto.Uf?.Trim().ToUpperInvariant();

            pedido.OrigemId = origem.Id;
            PreencherCampos(pedido, dto);

            if (enderecoMudou)
            {
                AplicarGeocodificacao(pedido);
            }

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoAtualizado", "Pedido atualizado", null, AutorEvento.Usuario, usuarioId);
            Outbox mensagem = FabricaEventos.DePedido(pedido, "PedidoAtualizado");

            _repository.Atualizar(pedido, evento, mensagem);

            return ParaDto(pedido);
        }

        public LerPedidoDto Cancelar(Guid id, CancelarPedidoDto dto, Guid usuarioId)
        {
            Pedido pedido = _repository.ObterPorId(id)
                ?? throw new DomainException("Pedido não encontrado.");

            if (pedido.Status != StatusPedido.Novo && pedido.Status != StatusPedido.Ocorrencia)
            {
                throw new DomainException("Só é possível cancelar pedidos novos ou com ocorrência. Se ele já está numa rota, tire-o da rota antes.");
            }

            pedido.Status = StatusPedido.Cancelado;

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoCancelado", "Pedido cancelado", StatusPedido.Cancelado, AutorEvento.Usuario, usuarioId);
            // O motivo fica nos dados do evento (não aparece no rastreio do cliente)
            evento.Dados = JsonSerializer.Serialize(new { motivo = dto.Motivo });
            Outbox mensagem = FabricaEventos.DePedido(pedido, "PedidoCancelado");

            _repository.Atualizar(pedido, evento, mensagem);

            return ParaDto(pedido);
        }

        public LerPedidoDto Geocodificar(Guid id)
        {
            Pedido pedido = _repository.ObterPorId(id)
                ?? throw new DomainException("Pedido não encontrado.");

            if (pedido.Status != StatusPedido.Novo)
            {
                throw new DomainException("Só é possível geocodificar pedidos com status novo.");
            }

            if (pedido.Modalidade == ModalidadePedido.Retirada)
            {
                throw new DomainException("Pedido de retirada na loja não precisa de localização.");
            }

            AplicarGeocodificacao(pedido);
            _repository.Atualizar(pedido);

            return ParaDto(pedido);
        }

        public LerPedidoDto AjustarCoordenadas(Guid id, AjustarCoordenadasDto dto, Guid usuarioId)
        {
            Pedido pedido = _repository.ObterPorId(id)
                ?? throw new DomainException("Pedido não encontrado.");

            if (pedido.Status != StatusPedido.Novo)
            {
                throw new DomainException("Só é possível ajustar a localização de pedidos com status novo.");
            }

            if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
            {
                throw new DomainException("Coordenadas inválidas.");
            }

            pedido.Latitude = Math.Round(dto.Latitude, 6);
            pedido.Longitude = Math.Round(dto.Longitude, 6);
            pedido.GeocodificacaoConfianca = 1.00m;
            pedido.GeocodificacaoStatus = StatusGeocodificacao.Ok;

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoLocalizacaoAjustada", "Localização de entrega ajustada", null, AutorEvento.Usuario, usuarioId);
            Outbox mensagem = FabricaEventos.DePedido(pedido, "PedidoLocalizacaoAjustada");

            _repository.Atualizar(pedido, evento, mensagem);

            return ParaDto(pedido);
        }

        public void CriarDaPlanilha(CriarPedidoDto dto, Guid empresaId, Guid usuarioId, Guid importacaoId, string dadosOriginais)
        {
            Validar(dto);
            _limitesPlano.ConferirNovoPedido(empresaId);

            string? idExterno = string.IsNullOrWhiteSpace(dto.NumeroExterno) ? null : dto.NumeroExterno.Trim();
            if (idExterno != null && _repository.ExisteIdExterno(CanalPedido.Planilha, idExterno))
            {
                throw new DomainException($"O pedido {idExterno} já foi importado antes.");
            }

            Pedido pedido = NovoPedido(dto, empresaId, CanalPedido.Planilha, idExterno);
            pedido.ImportacaoId = importacaoId;
            pedido.DadosOriginais = dadosOriginais;

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoCriado", "Pedido importado por planilha", StatusPedido.Novo, AutorEvento.Usuario, usuarioId);
            Outbox mensagem = FabricaEventos.DePedido(pedido, "PedidoCriado");

            _repository.Adicionar(pedido, evento, mensagem);
        }

        // Idempotente: o mesmo idExterno devolve o pedido já criado (Criado = false), sem duplicar
        public (Pedido Pedido, bool Criado) CriarPorIntegracao(CriarPedidoDto dto, string idExterno, Guid empresaId)
        {
            Pedido? existente = _repository.ObterPorIdExterno(CanalPedido.Api, idExterno);
            if (existente != null)
            {
                return (existente, false);
            }

            Validar(dto);
            _limitesPlano.ConferirNovoPedido(empresaId);
            Pedido pedido = NovoPedido(dto, empresaId, CanalPedido.Api, idExterno);

            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoCriado", "Pedido recebido pela integração", StatusPedido.Novo, AutorEvento.Sistema);
            _repository.Adicionar(pedido, evento, FabricaEventos.DePedido(pedido, "PedidoCriado"));

            return (_repository.ObterPorId(pedido.Id)!, true);
        }

        public Pedido? ObterPorIdExterno(string idExterno) => _repository.ObterPorIdExterno(CanalPedido.Api, idExterno.Trim());

        public List<Pedido> ListarAtualizadosDesde(DateTime desdeUtc, int quantidade) => _repository.ListarAtualizadosDesde(desdeUtc, quantidade);

        public Pedido CancelarPorIntegracao(string idExterno, string? motivo)
        {
            Pedido pedido = _repository.ObterPorIdExterno(CanalPedido.Api, idExterno.Trim())
                ?? throw new DomainException("Pedido não encontrado.");

            if (pedido.Status == StatusPedido.Cancelado)
            {
                return pedido;
            }

            if (pedido.Status != StatusPedido.Novo && pedido.Status != StatusPedido.Ocorrencia)
            {
                throw new DomainException("O pedido já está numa rota e não pode ser cancelado pela integração. Fale com a loja.");
            }

            pedido.Status = StatusPedido.Cancelado;
            PedidoEvento evento = FabricaEventos.LinhaDoTempo(pedido, "PedidoCancelado", "Pedido cancelado pela integração", StatusPedido.Cancelado, AutorEvento.Sistema);
            evento.Dados = JsonSerializer.Serialize(new { motivo });
            _repository.Atualizar(pedido, evento, FabricaEventos.DePedido(pedido, "PedidoCancelado"));
            return pedido;
        }

        // Parte comum de portal, planilha e integração. Quem chama já validou o dto e conferiu o limite do plano.
        private Pedido NovoPedido(CriarPedidoDto dto, Guid empresaId, CanalPedido canal, string? idExterno)
        {
            Origem origem = ObterOrigem(dto.OrigemId);

            var pedido = new Pedido
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                OrigemId = origem.Id,
                Canal = canal,
                IdExterno = idExterno,
                Status = StatusPedido.Novo,
                GeocodificacaoStatus = StatusGeocodificacao.Pendente,
                TokenRastreamento = GerarToken()
            };
            PreencherCampos(pedido, dto);
            AplicarGeocodificacao(pedido);
            return pedido;
        }

        private void AplicarGeocodificacao(Pedido pedido)
        {
            if (pedido.Modalidade == ModalidadePedido.Retirada)
            {
                return;
            }

            ResultadoGeocodificacao resultado = _geocodificador.Geocodificar(new EnderecoGeocodificacao
            {
                Logradouro = pedido.Logradouro,
                Numero = pedido.Numero,
                Bairro = pedido.Bairro,
                Cidade = pedido.Cidade,
                Uf = pedido.Uf,
                Cep = pedido.Cep
            });

            pedido.Latitude = resultado.Latitude;
            pedido.Longitude = resultado.Longitude;
            pedido.GeocodificacaoConfianca = resultado.Confianca;
            pedido.GeocodificacaoStatus = resultado.Status;
        }

        private static void Validar(CriarPedidoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ClienteNome))
            {
                throw new DomainException("Nome do cliente é obrigatório.");
            }

            if (!Enum.IsDefined(dto.Modalidade))
            {
                throw new DomainException("Modalidade inválida.");
            }

            if (dto.Modalidade != ModalidadePedido.Retirada)
            {
                if (string.IsNullOrWhiteSpace(dto.Logradouro)
                    || string.IsNullOrWhiteSpace(dto.Cidade)
                    || string.IsNullOrWhiteSpace(dto.Uf)
                    || dto.Uf.Trim().Length != 2)
                {
                    throw new DomainException("Endereço de entrega incompleto.");
                }

                string cep = SomenteDigitos(dto.Cep) ?? string.Empty;
                if (cep.Length != 0 && cep.Length != 8)
                {
                    throw new DomainException("CEP inválido.");
                }
            }

            if (dto.Modalidade == ModalidadePedido.Agendada && (dto.JanelaInicio == null || dto.JanelaFim == null))
            {
                throw new DomainException("Entrega agendada precisa de janela de entrega (início e fim).");
            }

            if (dto.JanelaInicio.HasValue && dto.JanelaFim.HasValue && dto.JanelaFim <= dto.JanelaInicio)
            {
                throw new DomainException("O fim da janela precisa ser depois do início.");
            }

            if (dto.PesoKg < 0)
            {
                throw new DomainException("Peso não pode ser negativo.");
            }

            if (dto.Volumes < 1)
            {
                throw new DomainException("O pedido precisa ter pelo menos 1 volume.");
            }
        }

        private Origem ObterOrigem(Guid? origemId)
        {
            // O filtro por empresa garante que não dá para usar a origem de outra loja
            Origem? origem = origemId.HasValue
                ? _origemRepository.ObterPorId(origemId.Value)
                : _origemRepository.ObterPadrao();

            if (origem == null || !origem.Ativa)
            {
                throw new DomainException("Origem não encontrada.");
            }

            return origem;
        }

        private static void PreencherCampos(Pedido pedido, CriarPedidoDto dto)
        {
            pedido.NumeroExterno = dto.NumeroExterno?.Trim();
            pedido.Modalidade = dto.Modalidade;
            pedido.ClienteNome = dto.ClienteNome.Trim();
            pedido.ClienteTelefone = dto.ClienteTelefone;
            pedido.ClienteEmail = dto.ClienteEmail?.Trim().ToLowerInvariant();
            pedido.Cep = SomenteDigitos(dto.Cep);
            pedido.Logradouro = dto.Logradouro?.Trim();
            pedido.Numero = dto.Numero?.Trim();
            pedido.Complemento = dto.Complemento;
            pedido.Bairro = dto.Bairro?.Trim();
            pedido.Cidade = dto.Cidade?.Trim();
            pedido.Uf = dto.Uf?.Trim().ToUpperInvariant();
            pedido.Referencia = dto.Referencia;
            pedido.JanelaInicio = dto.JanelaInicio?.UtcDateTime;
            pedido.JanelaFim = dto.JanelaFim?.UtcDateTime;
            pedido.PesoKg = dto.PesoKg;
            pedido.Volumes = dto.Volumes;
            pedido.Observacoes = dto.Observacoes;
        }

        private static string GerarToken()
        {
            // 24 bytes aleatórios em base64 "de URL" = 32 caracteres, impossível de adivinhar
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string? SomenteDigitos(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            return new string(texto.Where(char.IsDigit).ToArray());
        }

        private static LerPedidoDto ParaDto(Pedido pedido)
        {
            return new LerPedidoDto
            {
                Id = pedido.Id,
                NumeroPedido = pedido.NumeroPedido,
                NumeroExterno = pedido.NumeroExterno,
                Canal = pedido.Canal,
                Modalidade = pedido.Modalidade,
                Status = pedido.Status,
                OrigemId = pedido.OrigemId,
                OrigemNome = pedido.Origem?.Nome ?? string.Empty,
                ClienteNome = pedido.ClienteNome,
                ClienteTelefone = pedido.ClienteTelefone,
                ClienteEmail = pedido.ClienteEmail,
                Cep = pedido.Cep,
                Logradouro = pedido.Logradouro,
                Numero = pedido.Numero,
                Complemento = pedido.Complemento,
                Bairro = pedido.Bairro,
                Cidade = pedido.Cidade,
                Uf = pedido.Uf,
                Referencia = pedido.Referencia,
                Latitude = pedido.Latitude,
                Longitude = pedido.Longitude,
                GeocodificacaoStatus = pedido.GeocodificacaoStatus,
                JanelaInicio = pedido.JanelaInicio,
                JanelaFim = pedido.JanelaFim,
                PesoKg = pedido.PesoKg,
                Volumes = pedido.Volumes,
                Observacoes = pedido.Observacoes,
                TokenRastreamento = pedido.TokenRastreamento,
                EntregueEm = pedido.EntregueEm,
                CriadoEm = pedido.CriadoEm,
                AtualizadoEm = pedido.AtualizadoEm,
                Eventos = pedido.PedidoEventos
                    .OrderBy(e => e.OcorridoEm)
                    .Select(e => new LerPedidoEventoDto
                    {
                        TipoEvento = e.TipoEvento,
                        StatusNovo = e.StatusNovo,
                        Descricao = e.Descricao,
                        AutorTipo = e.AutorTipo,
                        OcorridoEm = e.OcorridoEm
                    }).ToList()
            };
        }
    }
}
