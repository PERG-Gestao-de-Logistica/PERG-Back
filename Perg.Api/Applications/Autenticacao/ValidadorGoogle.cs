using Google.Apis.Auth;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Autenticacao
{
    public class ValidadorGoogle : IValidadorGoogle
    {
        private readonly string? _clientId;

        public ValidadorGoogle(IConfiguration config)
        {
            _clientId = config["Google:ClientId"];
        }

        public ContaGoogle Validar(string idToken)
        {
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                throw new DomainException("Login com Google não configurado (Google:ClientId).");
            }

            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new DomainException("Token do Google não enviado.");
            }

            try
            {
                var configuracao = new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] };
                GoogleJsonWebSignature.Payload dados = GoogleJsonWebSignature.ValidateAsync(idToken, configuracao).GetAwaiter().GetResult();
                return new ContaGoogle(dados.Subject, dados.Email.Trim().ToLowerInvariant(), dados.EmailVerified, dados.Name);
            }
            catch (InvalidJwtException)
            {
                throw new DomainException("Login com Google inválido ou expirado. Tente de novo.");
            }
        }
    }
}
