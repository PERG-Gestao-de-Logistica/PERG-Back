using Perg.Api.Domains;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Confirmação do celular do entregador por código de 6 dígitos (mesmas regras do esqueci a senha:
    // 15 min, 5 tentativas, reenvio a cada 1 min). Feita logo após o cadastro, com o app em análise;
    // o painel só aprova o cadastro com o celular confirmado.
    public class ConfirmacaoTelefoneService
    {
        private readonly IEntregadorRepository _repository;
        private readonly ICodigosVerificacao _codigos;
        private readonly IEnvioSms _sms;

        public ConfirmacaoTelefoneService(IEntregadorRepository repository, ICodigosVerificacao codigos, IEnvioSms sms)
        {
            _repository = repository;
            _codigos = codigos;
            _sms = sms;
        }

        public string EnviarCodigo(Guid entregadorId)
        {
            Entregador entregador = _repository.ObterPorId(entregadorId) ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.TelefoneConfirmado)
            {
                throw new DomainException("Seu celular já está confirmado.");
            }

            string? codigo = _codigos.Gerar(Chave(entregadorId));
            if (codigo == null)
            {
                throw new DomainException("Já enviamos um código há menos de 1 minuto. Aguarde para pedir outro.");
            }

            if (!_sms.Enviar(entregador.Telefone, $"Perg: seu código de confirmação é {codigo}. Ele vale por 15 minutos. Não compartilhe."))
            {
                throw new DomainException("Não foi possível enviar o SMS agora. Tente de novo em instantes.");
            }

            return Mascarar(entregador.Telefone);
        }

        public void Confirmar(Guid entregadorId, string codigo)
        {
            Entregador entregador = _repository.ObterPorId(entregadorId) ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.TelefoneConfirmado)
            {
                return;
            }

            switch (_codigos.Validar(Chave(entregadorId), codigo))
            {
                case ResultadoCodigo.Valido:
                    break;
                case ResultadoCodigo.Invalido:
                    throw new DomainException("Código incorreto.");
                case ResultadoCodigo.TentativasEsgotadas:
                    throw new DomainException("Muitas tentativas erradas. Peça um novo código.");
                default:
                    throw new DomainException("Código expirado. Peça um novo código.");
            }

            entregador.TelefoneConfirmado = true;
            _repository.Salvar();
        }

        private static string Chave(Guid entregadorId) => "telefone:entregador:" + entregadorId;

        // (11) 9****-7777: confirma para qual número foi, sem mostrar o número inteiro
        private static string Mascarar(string telefone)
        {
            if (telefone.Length < 10) return telefone;
            return $"({telefone[..2]}) {telefone[2]}****-{telefone[^4..]}";
        }
    }
}
