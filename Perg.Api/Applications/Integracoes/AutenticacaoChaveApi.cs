using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Integracoes
{
    // Esquema "ApiKey": sistemas de vendas mandam o cabeçalho X-Api-Key.
    // A identidade criada tem empresa_id (o filtro por empresa funciona igual ao do portal) e chave_api_id.
    public class AutenticacaoChaveApi : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Esquema = "ApiKey";
        public const string ClaimChaveApiId = "chave_api_id";

        private readonly IIntegracaoRepository _repository;

        public AutenticacaoChaveApi(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            IIntegracaoRepository repository)
            : base(options, logger, encoder)
        {
            _repository = repository;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? chave = Request.Headers[ChavesApi.Cabecalho].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(chave))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ChaveApi? registro = _repository.ObterChaveAtivaPorHash(ChavesApi.Hash(chave));
            if (registro == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Chave de API inválida ou revogada."));
            }

            _repository.RegistrarUsoDaChave(registro);

            var identidade = new ClaimsIdentity(new[]
            {
                new Claim(GeradorTokenJwt.ClaimEmpresaId, registro.EmpresaId.ToString()),
                new Claim(ClaimChaveApiId, registro.Id.ToString()),
                new Claim(ClaimTypes.Name, registro.Nome)
            }, Esquema);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { mensagem = "Envie uma chave de API válida no cabeçalho X-Api-Key." });
        }
    }
}
