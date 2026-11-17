using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Autenticacao
{
    public static class ConferenciaCodigo
    {
        public static void Conferir(this ICodigosVerificacao codigos, string chave, string? codigo)
        {
            switch (codigos.Validar(chave, codigo ?? string.Empty))
            {
                case ResultadoCodigo.Valido:
                    return;
                case ResultadoCodigo.Invalido:
                    throw new DomainException("Código incorreto.");
                case ResultadoCodigo.TentativasEsgotadas:
                    throw new DomainException("Muitas tentativas erradas. Peça um novo código.");
                default:
                    throw new DomainException("Código inválido ou expirado. Peça um novo código.");
            }
        }
    }
}
