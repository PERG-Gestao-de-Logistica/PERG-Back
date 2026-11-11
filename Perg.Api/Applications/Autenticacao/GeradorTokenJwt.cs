using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Exceptions;

namespace Perg.Api.Applications.Autenticacao
{
    public class GeradorTokenJwt
    {
        // Nome da claim com a empresa do usuário (usada no filtro por empresa)
        public const string ClaimEmpresaId = "empresa_id";

        // Claim que identifica o token do app do entregador
        public const string ClaimEntregadorId = "entregador_id";

        // Claim que identifica o token do backoffice (equipe interna do Perg)
        public const string ClaimAdministradorId = "administrador_id";

        private readonly IConfiguration _config;

        public GeradorTokenJwt(IConfiguration config)
        {
            _config = config;
        }

        public string GerarToken(Usuario usuario)
        {
            return Gerar(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, ConversorEnumSnakeCase<PapelUsuario>.ParaTexto(usuario.Papel)),
                new Claim(ClaimEmpresaId, usuario.EmpresaId.ToString())
            });
        }

        // Token do app: não tem empresa_id, então não passa pelas rotas da loja
        public string GerarTokenEntregador(Entregador entregador)
        {
            return Gerar(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, entregador.Id.ToString()),
                new Claim(ClaimTypes.Name, entregador.Nome),
                new Claim(ClaimTypes.Role, "entregador"),
                new Claim(ClaimEntregadorId, entregador.Id.ToString())
            });
        }

        // Token do backoffice: sem empresa_id nem entregador_id, só passa nas rotas api/backoffice
        public string GerarTokenAdministrador(Administrador administrador)
        {
            return Gerar(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, administrador.Id.ToString()),
                new Claim(ClaimTypes.Name, administrador.Nome),
                new Claim(ClaimTypes.Email, administrador.Email),
                new Claim(ClaimTypes.Role, "backoffice"),
                new Claim(ClaimAdministradorId, administrador.Id.ToString())
            });
        }

        private string Gerar(List<Claim> claims)
        {
            var chave = _config["Jwt:Key"]!;
            var issuer = _config["Jwt:Issuer"]!;
            var audience = _config["Jwt:Audience"]!;
            var expiraEmMinutos = int.Parse(_config["Jwt:ExpiraEmMinutos"]!);

            var keyBytes = Encoding.UTF8.GetBytes(chave);

            // Segurança: exige uma chave com pelo menos 32 caracteres (256 bits)
            if (keyBytes.Length < 32)
            {
                throw new DomainException("Jwt: Key precisa ter pelo menos 32 caracteres (256 bits).");
            }

            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiraEmMinutos),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
