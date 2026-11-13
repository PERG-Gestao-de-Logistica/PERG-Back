using System.Security.Cryptography;
using System.Text.Json;
using Perg.Api.Applications.Integracoes;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.IntegracoesDto;
using Perg.Api.DTOs.PedidosDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Integração genérica com sistemas de vendas (ERP, loja virtual, PDV):
    //  - entrada: API pública /api/integracao/v1 autenticada por chave de API (X-Api-Key);
    //  - saída: webhooks com os eventos dos pedidos, assinados com HMAC-SHA256.
    // A loja gerencia chaves e webhooks pelo portal (só admin).
    public class IntegracaoService
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private readonly IIntegracaoRepository _repository;
        private readonly IUsuarioRepository _usuarios;
        private readonly PedidoService _pedidos;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _ambiente;

        public IntegracaoService(IIntegracaoRepository repository, IUsuarioRepository usuarios, PedidoService pedidos,
            IConfiguration config, IWebHostEnvironment ambiente)
        {
            _repository = repository;
            _usuarios = usuarios;
            _pedidos = pedidos;
            _config = config;
            _ambiente = ambiente;
        }

        // ---------- API pública: pedidos ----------

        public (LerPedidoIntegracaoDto Pedido, bool Criado) CriarPedido(CriarPedidoIntegracaoDto dto, Guid empresaId)
        {
            string idExterno = (dto.IdExterno ?? string.Empty).Trim();
            if (idExterno.Length == 0 || idExterno.Length > 100)
            {
                throw new DomainException("Informe o idExterno (id do pedido no seu sistema, até 100 caracteres).");
            }

            var criar = new CriarPedidoDto
            {
                OrigemId = dto.OrigemId,
                NumeroExterno = dto.NumeroExterno,
                Modalidade = dto.Modalidade,
                ClienteNome = dto.Cliente?.Nome ?? string.Empty,
                ClienteTelefone = dto.Cliente?.Telefone,
                ClienteEmail = dto.Cliente?.Email,
                Cep = dto.Endereco?.Cep,
                Logradouro = dto.Endereco?.Logradouro,
                Numero = dto.Endereco?.Numero,
                Complemento = dto.Endereco?.Complemento,
                Bairro = dto.Endereco?.Bairro,
                Cidade = dto.Endereco?.Cidade,
                Uf = dto.Endereco?.Uf,
                Referencia = dto.Endereco?.Referencia,
                JanelaInicio = dto.JanelaInicio,
                JanelaFim = dto.JanelaFim,
                PesoKg = dto.PesoKg,
                Volumes = dto.Volumes,
                Observacoes = dto.Observacoes
            };

            (Pedido pedido, bool criado) = _pedidos.CriarPorIntegracao(criar, idExterno, empresaId);
            return (ParaDto(pedido, LinkRastreio(pedido)), criado);
        }

        public LerPedidoIntegracaoDto ObterPedido(string idExterno)
        {
            Pedido pedido = _pedidos.ObterPorIdExterno(idExterno) ?? throw new DomainException("Pedido não encontrado.");
            return ParaDto(pedido, LinkRastreio(pedido));
        }

        // Para quem prefere consultar em vez de receber webhook: tudo que mudou desde um horário
        public List<LerPedidoIntegracaoDto> ListarAtualizados(DateTimeOffset desde)
        {
            return _pedidos.ListarAtualizadosDesde(desde.UtcDateTime, 200).Select(p => ParaDto(p, LinkRastreio(p))).ToList();
        }

        public LerPedidoIntegracaoDto CancelarPedido(string idExterno, CancelarPedidoIntegracaoDto dto)
        {
            Pedido pedido = _pedidos.CancelarPorIntegracao(idExterno, dto.Motivo);
            return ParaDto(pedido, LinkRastreio(pedido));
        }

        // ---------- Gestão: chaves de API ----------

        public List<LerChaveApiDto> ListarChaves(Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            return _repository.ListarChaves().Select(ParaDto).ToList();
        }

        public (LerChaveApiDto Chave, string ChaveCompleta) CriarChave(CriarChaveApiDto dto, Guid empresaId, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);

            string nome = (dto.Nome ?? string.Empty).Trim();
            if (nome.Length == 0 || nome.Length > 100) throw new DomainException("Dê um nome para a chave (ex.: \"ERP\", \"Site\").");

            string chaveCompleta = ChavesApi.Gerar();
            var chave = new ChaveApi
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                Nome = nome,
                Prefixo = ChavesApi.Inicio(chaveCompleta),
                HashChave = ChavesApi.Hash(chaveCompleta),
                CriadoPor = usuarioId,
                CriadoEm = DateTime.UtcNow
            };
            _repository.AdicionarChave(chave);
            return (ParaDto(chave), chaveCompleta);
        }

        public void RevogarChave(Guid id, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            ChaveApi chave = _repository.ObterChave(id) ?? throw new DomainException("Chave não encontrada.");
            chave.RevogadaEm ??= DateTime.UtcNow;
            _repository.Salvar();
        }

        // ---------- Gestão: webhooks ----------

        public List<LerWebhookDto> ListarWebhooks(Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            return _repository.ListarWebhooks().Select(w => ParaDto(w, false)).ToList();
        }

        public LerWebhookDto CriarWebhook(SalvarWebhookDto dto, Guid empresaId, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            if (_repository.ListarWebhooks().Count >= 5) throw new DomainException("Limite de 5 webhooks por loja.");

            var webhook = new Webhook
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                Url = ValidarUrl(dto.Url),
                Segredo = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
                Ativo = true
            };
            _repository.AdicionarWebhook(webhook);
            return ParaDto(webhook, true);
        }

        public LerWebhookDto AtualizarWebhook(Guid id, SalvarWebhookDto dto, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            Webhook webhook = _repository.ObterWebhook(id) ?? throw new DomainException("Webhook não encontrado.");
            webhook.Url = ValidarUrl(dto.Url);
            webhook.Ativo = dto.Ativo;
            if (dto.Ativo) webhook.FalhasSeguidas = 0;
            _repository.Salvar();
            return ParaDto(webhook, false);
        }

        public void RemoverWebhook(Guid id, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            Webhook webhook = _repository.ObterWebhook(id) ?? throw new DomainException("Webhook não encontrado.");
            _repository.RemoverWebhook(webhook);
        }

        // Manda um evento "ping" para conferir se o endereço recebe e valida a assinatura
        public void TestarWebhook(Guid id, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            Webhook webhook = _repository.ObterWebhook(id) ?? throw new DomainException("Webhook não encontrado.");

            var entrega = NovaEntrega(webhook, "ping", new { mensagem = "Teste do webhook do Perg." });
            _repository.AdicionarEntregas([entrega]);
        }

        public List<LerWebhookEntregaDto> ListarEntregas(Guid id, Guid usuarioId)
        {
            ExigirAdmin(usuarioId);
            _ = _repository.ObterWebhook(id) ?? throw new DomainException("Webhook não encontrado.");
            return _repository.ListarEntregas(id, 50).Select(e => new LerWebhookEntregaDto
            {
                Id = e.Id,
                Evento = e.Evento,
                Status = e.Status,
                Tentativas = e.Tentativas,
                UltimoStatusHttp = e.UltimoStatusHttp,
                UltimoErro = e.UltimoErro,
                CriadoEm = e.CriadoEm,
                EntregueEm = e.EntregueEm,
                ProximaTentativaEm = e.ProximaTentativaEm
            }).ToList();
        }

        // ---------- apoio (também usado pelo consumidor da outbox) ----------

        public static WebhookEntrega NovaEntrega(Webhook webhook, string evento, object dados)
        {
            var id = Guid.CreateVersion7();
            DateTime agora = DateTime.UtcNow;
            string payload = JsonSerializer.Serialize(new { id, evento, criadoEm = agora, dados }, Json);

            return new WebhookEntrega
            {
                Id = id,
                WebhookId = webhook.Id,
                EmpresaId = webhook.EmpresaId,
                Evento = evento,
                Payload = payload,
                Status = StatusWebhookEntrega.Pendente,
                ProximaTentativaEm = agora,
                CriadoEm = agora
            };
        }

        public string LinkRastreio(Pedido pedido) =>
            (_config["Front:UrlRastreio"] ?? "http://localhost:3000/rastreio/{token}").Replace("{token}", Uri.EscapeDataString(pedido.TokenRastreamento));

        public static LerPedidoIntegracaoDto ParaDto(Pedido p, string linkRastreio) => new()
        {
            Id = p.Id,
            IdExterno = p.IdExterno,
            NumeroPedido = p.NumeroPedido,
            NumeroExterno = p.NumeroExterno,
            Status = p.Status,
            Modalidade = p.Modalidade,
            GeocodificacaoStatus = p.GeocodificacaoStatus,
            LinkRastreio = linkRastreio,
            JanelaInicio = p.JanelaInicio,
            JanelaFim = p.JanelaFim,
            EntregueEm = p.EntregueEm,
            CriadoEm = p.CriadoEm,
            AtualizadoEm = p.AtualizadoEm
        };

        // HTTPS obrigatório (em desenvolvimento também aceita http://localhost)
        private string ValidarUrl(string? url)
        {
            if (!Uri.TryCreate((url ?? string.Empty).Trim(), UriKind.Absolute, out Uri? uri))
            {
                throw new DomainException("URL inválida.");
            }

            bool localDev = _ambiente.IsDevelopment() && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp;
            if (uri.Scheme != Uri.UriSchemeHttps && !localDev)
            {
                throw new DomainException("O webhook precisa usar https://.");
            }

            return uri.ToString();
        }

        private void ExigirAdmin(Guid usuarioId)
        {
            Usuario? usuario = _usuarios.ObterPorId(usuarioId);
            if (usuario == null || !usuario.Ativo || usuario.Papel != PapelUsuario.Admin)
            {
                throw new AcessoNegadoException("Só o administrador da empresa gerencia as integrações.");
            }
        }

        private static LerChaveApiDto ParaDto(ChaveApi c) => new()
        {
            Id = c.Id,
            Nome = c.Nome,
            Inicio = c.Prefixo + "...",
            Ativa = c.RevogadaEm == null,
            CriadoEm = c.CriadoEm,
            UltimoUsoEm = c.UltimoUsoEm
        };

        private static LerWebhookDto ParaDto(Webhook w, bool comSegredo) => new()
        {
            Id = w.Id,
            Url = w.Url,
            Ativo = w.Ativo,
            FalhasSeguidas = w.FalhasSeguidas,
            CriadoEm = w.CriadoEm,
            Segredo = comSegredo ? w.Segredo : null
        };
    }
}
