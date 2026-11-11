using Perg.Api.Applications.Assinaturas;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.AssinaturasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Plano e assinatura da loja.
    // Plano grátis: sem cobrança. Plano pago: assinatura mensal no Asaas, primeira fatura vence no dia da troca.
    // Os webhooks de pagamento mantêm o status: pago → Ativa (próximo vencimento + 1 mês); atrasado → Inadimplente.
    public class AssinaturaService
    {
        private readonly IAssinaturaRepository _repository;
        private readonly IUsuarioRepository _usuarios;
        private readonly IEmpresaRepository _empresas;
        private readonly ICobrancaAssinatura _cobranca;
        private readonly LimitesPlano _limites;
        private readonly NotificadorTempoReal _tempoReal;
        private readonly ILogger<AssinaturaService> _logger;

        public AssinaturaService(IAssinaturaRepository repository, IUsuarioRepository usuarios, IEmpresaRepository empresas,
            ICobrancaAssinatura cobranca, LimitesPlano limites, NotificadorTempoReal tempoReal, ILogger<AssinaturaService> logger)
        {
            _repository = repository;
            _usuarios = usuarios;
            _empresas = empresas;
            _cobranca = cobranca;
            _limites = limites;
            _tempoReal = tempoReal;
            _logger = logger;
        }

        public List<LerPlanoDto> ListarPlanos()
        {
            return _repository.ListarPlanosAtivos().Select(ParaDto).ToList();
        }

        // Empresa sem assinatura (cadastrada antes deste módulo) entra no plano grátis aqui
        public LerAssinaturaDto Obter(Guid empresaId)
        {
            Assinatura assinatura = ObterOuCriarGratis(empresaId);
            return ParaDto(assinatura, empresaId, LinkPendente(assinatura));
        }

        public LerAssinaturaDto TrocarPlano(Guid empresaId, Guid usuarioId, TrocarPlanoDto dto)
        {
            ExigirAdmin(usuarioId);

            Plano plano = _repository.ObterPlano(dto.PlanoId) ?? throw new DomainException("Plano não encontrado.");
            if (!plano.Ativo) throw new DomainException("Este plano não está mais disponível.");

            Assinatura atual = ObterOuCriarGratis(empresaId);
            if (atual.PlanoId == plano.Id)
            {
                throw new DomainException($"A loja já está no plano {plano.Nome}.");
            }

            // Limite menor que o já usado no mês: não deixa descer agora
            if (plano.LimitePedidosMes.HasValue && _limites.PedidosNoMes(empresaId) > plano.LimitePedidosMes.Value)
            {
                throw new DomainException($"A loja já criou mais de {plano.LimitePedidosMes} pedidos este mês. A troca para o plano {plano.Nome} pode ser feita no próximo mês.");
            }

            var nova = new Assinatura
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                PlanoId = plano.Id,
                Status = StatusAssinatura.Ativa
            };

            string? link = null;
            if (plano.PrecoMensal > 0)
            {
                // Primeiro cria no Asaas: se falhar, nada muda na loja
                Empresa empresa = _empresas.ObterPorId(empresaId) ?? throw new DomainException("Empresa não encontrada.");
                AssinaturaNoGateway gateway = _cobranca.Criar(empresa, plano, nova.Id);
                nova.Gateway = gateway.Gateway;
                nova.GatewayAssinaturaId = gateway.IdNoGateway;
                nova.ProximoVencimento = gateway.ProximoVencimento;
                link = gateway.LinkPagamento;
            }

            string? cobrancaAntiga = atual.GatewayAssinaturaId;
            _repository.Trocar(atual, nova);

            if (cobrancaAntiga != null)
            {
                _cobranca.Cancelar(cobrancaAntiga);
            }

            nova.Plano = plano;
            return ParaDto(nova, empresaId, link);
        }

        // Cancelar o plano pago = voltar para o grátis
        public LerAssinaturaDto Cancelar(Guid empresaId, Guid usuarioId)
        {
            Plano gratis = _repository.ObterPlanoGratis() ?? throw new DomainException("Plano grátis não cadastrado.");
            return TrocarPlano(empresaId, usuarioId, new TrocarPlanoDto { PlanoId = gratis.Id });
        }

        // Só no sandbox/desenvolvimento: dá baixa na fatura em aberto e aplica o pagamento já (sem esperar o webhook)
        public LerAssinaturaDto SimularPagamento(Guid empresaId)
        {
            Assinatura assinatura = _repository.ObterVigente(empresaId) ?? throw new DomainException("A loja não tem assinatura.");
            if (assinatura.GatewayAssinaturaId == null) throw new DomainException("O plano atual não tem cobrança.");

            if (!_cobranca.MarcarPendenteComoPaga(assinatura.GatewayAssinaturaId))
            {
                throw new DomainException("Não há fatura em aberto nesta assinatura.");
            }

            AplicarPagamento(assinatura, DateOnly.FromDateTime(DateTime.UtcNow));
            _repository.Salvar();
            return ParaDto(assinatura, empresaId, null);
        }

        // ---------- Webhook do Asaas (eventos PAYMENT_*) ----------

        public void ProcessarEventoPagamento(string evento, string? assinaturaNoGateway, string? vencimento)
        {
            if (string.IsNullOrWhiteSpace(assinaturaNoGateway)) return; // cobrança avulsa, não é assinatura

            Assinatura? assinatura = _repository.ObterPorIdNoGateway(assinaturaNoGateway);
            if (assinatura == null || assinatura.Status == StatusAssinatura.Cancelada)
            {
                _logger.LogInformation("Webhook {Evento} para assinatura {Id} desconhecida ou cancelada.", evento, assinaturaNoGateway);
                return;
            }

            DateOnly dataVencimento = DateOnly.TryParse(vencimento, out DateOnly v) ? v : DateOnly.FromDateTime(DateTime.UtcNow);

            switch (evento)
            {
                case "PAYMENT_RECEIVED":
                case "PAYMENT_CONFIRMED":
                    AplicarPagamento(assinatura, dataVencimento);
                    break;

                case "PAYMENT_OVERDUE":
                    if (assinatura.Status != StatusAssinatura.Inadimplente)
                    {
                        assinatura.Status = StatusAssinatura.Inadimplente;
                        Notificar(assinatura.EmpresaId, $"Pagamento do plano {assinatura.Plano.Nome} em atraso. Novos pedidos ficam bloqueados até a regularização.");
                    }
                    break;

                default:
                    return;
            }

            _repository.Salvar();
        }

        // ---------- apoio ----------

        private void AplicarPagamento(Assinatura assinatura, DateOnly vencimentoPago)
        {
            DateOnly proximo = vencimentoPago.AddMonths(1);
            if (assinatura.ProximoVencimento == null || proximo > assinatura.ProximoVencimento)
            {
                assinatura.ProximoVencimento = proximo;
            }

            if (assinatura.Status == StatusAssinatura.Inadimplente)
            {
                Notificar(assinatura.EmpresaId, "Pagamento recebido. A assinatura do Perg está em dia de novo.");
            }
            assinatura.Status = StatusAssinatura.Ativa;
        }

        private Assinatura ObterOuCriarGratis(Guid empresaId)
        {
            Assinatura? vigente = _repository.ObterVigente(empresaId);
            if (vigente != null) return vigente;

            Plano gratis = _repository.ObterPlanoGratis() ?? throw new DomainException("Plano grátis não cadastrado.");
            var assinatura = new Assinatura
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                PlanoId = gratis.Id,
                Status = StatusAssinatura.Ativa
            };
            _repository.Trocar(null, assinatura);
            assinatura.Plano = gratis;
            return assinatura;
        }

        private void ExigirAdmin(Guid usuarioId)
        {
            Usuario? usuario = _usuarios.ObterPorId(usuarioId);
            if (usuario == null || !usuario.Ativo || usuario.Papel != PapelUsuario.Admin)
            {
                throw new AcessoNegadoException("Só o administrador da empresa pode mudar o plano.");
            }
        }

        private string? LinkPendente(Assinatura assinatura)
        {
            if (assinatura.GatewayAssinaturaId == null) return null;
            try
            {
                return _cobranca.ObterLinkPendente(assinatura.GatewayAssinaturaId);
            }
            catch (DomainException)
            {
                return null; // Asaas fora do ar não impede de mostrar o plano
            }
        }

        private void Notificar(Guid empresaId, string titulo)
        {
            var notificacoes = _repository.ListarUsuariosParaCobranca(empresaId)
                .Select(usuarioId => new Notificacao
                {
                    Id = Guid.CreateVersion7(),
                    EmpresaId = empresaId,
                    UsuarioId = usuarioId,
                    Tipo = TipoNotificacao.Cobranca,
                    Titulo = titulo,
                    Link = "/configuracoes/plano",
                    CriadoEm = DateTime.UtcNow
                })
                .ToList();

            foreach (Notificacao n in notificacoes) _repository.AdicionarNotificacao(n);
            _repository.Salvar();
            _tempoReal.EnviarNotificacoes(notificacoes);
        }

        private LerAssinaturaDto ParaDto(Assinatura a, Guid empresaId, string? link)
        {
            return new LerAssinaturaDto
            {
                Plano = ParaDto(a.Plano),
                Status = StatusAssinatura.Texto(a.Status),
                ProximoVencimento = a.ProximoVencimento,
                PedidosNoMes = _limites.PedidosNoMes(empresaId),
                LinkPagamento = link
            };
        }

        private static LerPlanoDto ParaDto(Plano p) => new()
        {
            Id = p.Id,
            Nome = p.Nome,
            PrecoMensal = p.PrecoMensal,
            LimitePedidosMes = p.LimitePedidosMes
        };
    }
}
