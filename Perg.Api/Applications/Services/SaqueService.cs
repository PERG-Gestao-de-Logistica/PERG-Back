using Perg.Api.Applications.Pagamentos;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.SaquesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Saque da carteira do entregador via Pix (Asaas).
    // O débito na carteira acontece antes de chamar o Asaas; se a transferência falhar, o valor é estornado.
    public class SaqueService
    {
        private const decimal ValorMinimo = 10m;

        private readonly ISaqueRepository _repository;
        private readonly IGatewayPagamento _gateway;
        private readonly ILogger<SaqueService> _logger;

        public SaqueService(ISaqueRepository repository, IGatewayPagamento gateway, ILogger<SaqueService> logger)
        {
            _repository = repository;
            _gateway = gateway;
            _logger = logger;
        }

        public string AtualizarChavePix(Guid entregadorId, AtualizarChavePixDto dto)
        {
            ChavePixNormalizada chave = ChavePix.Normalizar(dto.ChavePix)
                ?? throw new DomainException("Chave Pix inválida. Use CPF, CNPJ, e-mail, celular ou chave aleatória.");

            Entregador entregador = _repository.ObterEntregador(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            entregador.ChavePix = chave.Valor;
            _repository.Salvar();
            return chave.Valor;
        }

        public LerSaqueDto Solicitar(Guid entregadorId, SolicitarSaqueDto dto)
        {
            decimal valor = Math.Round(dto.Valor, 2);

            if (valor < ValorMinimo)
            {
                throw new DomainException($"O valor mínimo para saque é R$ {ValorMinimo:N2}.");
            }

            Entregador entregador = _repository.ObterEntregador(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.Status != StatusEntregador.Ativo)
            {
                throw new DomainException("Seu cadastro precisa estar ativo para sacar.");
            }

            ChavePixNormalizada chave = ChavePix.Normalizar(entregador.ChavePix)
                ?? throw new DomainException("Cadastre uma chave Pix válida antes de sacar.");

            if (_repository.TemSaqueEmAndamento(entregadorId))
            {
                throw new DomainException("Você já tem um saque em andamento. Aguarde a conclusão.");
            }

            // 1. Débito na carteira, com trava contra saque duplo
            Saque saque = _repository.RegistrarSaque(entregadorId, valor, chave.Valor)
                ?? throw new DomainException("Saldo insuficiente.");

            // 2. Pix pelo gateway
            ResultadoTransferencia resultado = _gateway.TransferirPix(saque.Id, valor, chave, "Saque Perg");

            Saque rastreado = _repository.ObterPorId(saque.Id)!;
            rastreado.GatewayTransferenciaId = resultado.IdNoGateway;
            AplicarResultado(rastreado, resultado.Situacao, resultado.Erro);
            _repository.Salvar();

            return ParaDto(rastreado);
        }

        public List<LerSaqueDto> Listar(Guid entregadorId)
        {
            return _repository.ListarDoEntregador(entregadorId, 30).Select(ParaDto).ToList();
        }

        // ---------- Webhooks do Asaas ----------

        // Evento de transferência (TRANSFER_DONE, TRANSFER_FAILED, TRANSFER_CANCELLED...)
        public void ProcessarEventoTransferencia(string evento, string? idNoGateway, string? referencia, string? motivoFalha)
        {
            Saque? saque = BuscarSaque(idNoGateway, referencia);
            if (saque == null)
            {
                _logger.LogWarning("Webhook Asaas {Evento} sem saque correspondente ({Id}).", evento, idNoGateway);
                return;
            }

            if (saque.GatewayTransferenciaId == null && idNoGateway != null)
            {
                saque.GatewayTransferenciaId = idNoGateway;
            }

            switch (evento)
            {
                case "TRANSFER_DONE":
                    AplicarResultado(saque, SituacaoTransferencia.Concluida, null);
                    break;
                case "TRANSFER_FAILED":
                case "TRANSFER_CANCELLED":
                    AplicarResultado(saque, SituacaoTransferencia.Falhou, motivoFalha ?? "Transferência não concluída.");
                    break;
                default:
                    return; // eventos intermediários (criada, em processamento): nada a fazer
            }

            _repository.Salvar();
        }

        // Validação de saque: o Asaas pergunta se a transferência é mesmo nossa antes de enviar
        public bool AprovarTransferencia(string? idNoGateway, string? referencia, decimal valor)
        {
            Saque? saque = BuscarSaque(idNoGateway, referencia);
            return saque != null
                && saque.Valor == valor
                && (saque.Status == StatusSaque.Solicitado || saque.Status == StatusSaque.Processando);
        }

        // ---------- Simulação do sandbox ----------

        // O sandbox do Asaas não conclui Pix: depois de alguns segundos, tratamos o saque como se
        // o webhook TRANSFER_DONE tivesse chegado (mesmo caminho do webhook) e cancelamos a pendência no Asaas.
        public int ConcluirSaquesDoSandbox(TimeSpan espera)
        {
            List<Saque> pendentes = _repository.ListarProcessandoAntesDe(DateTime.UtcNow - espera);

            foreach (Saque saque in pendentes)
            {
                ProcessarEventoTransferencia("TRANSFER_DONE", saque.GatewayTransferenciaId, saque.Id.ToString(), null);
                _logger.LogInformation("Saque {Saque} de R$ {Valor} concluído pela simulação do sandbox.", saque.Id, saque.Valor);

                // Já está "pago" aqui; o TRANSFER_CANCELLED que o Asaas mandar depois é ignorado
                if (!string.IsNullOrWhiteSpace(saque.GatewayTransferenciaId))
                {
                    _gateway.CancelarTransferencia(saque.GatewayTransferenciaId);
                }
            }

            return pendentes.Count;
        }

        // ---------- apoio ----------

        private Saque? BuscarSaque(string? idNoGateway, string? referencia)
        {
            if (Guid.TryParse(referencia, out Guid saqueId))
            {
                Saque? porReferencia = _repository.ObterPorId(saqueId);
                if (porReferencia != null) return porReferencia;
            }

            return string.IsNullOrWhiteSpace(idNoGateway) ? null : _repository.ObterPorIdNoGateway(idNoGateway);
        }

        // Só muda de estado a partir de "solicitado"/"processando": eventos repetidos não fazem nada
        private void AplicarResultado(Saque saque, SituacaoTransferencia situacao, string? erro)
        {
            if (saque.Status is StatusSaque.Pago or StatusSaque.Falhou)
            {
                return;
            }

            switch (situacao)
            {
                case SituacaoTransferencia.Concluida:
                    saque.Status = StatusSaque.Pago;
                    saque.PagoEm = DateTime.UtcNow;
                    saque.Erro = null;
                    break;
                case SituacaoTransferencia.Falhou:
                    saque.Status = StatusSaque.Falhou;
                    saque.Erro = erro;
                    _repository.Estornar(saque, erro ?? "falha na transferência");
                    break;
                default:
                    saque.Status = StatusSaque.Processando;
                    saque.Erro = erro;
                    break;
            }
        }

        private static LerSaqueDto ParaDto(Saque s)
        {
            return new LerSaqueDto
            {
                Id = s.Id,
                Valor = s.Valor,
                ChavePix = s.ChavePix,
                Status = s.Status,
                Erro = s.Erro,
                SolicitadoEm = s.SolicitadoEm,
                PagoEm = s.PagoEm
            };
        }
    }
}
