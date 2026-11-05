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

        private readonly IConfiguration _config;

        public GeradorTokenJwt(IConfiguration config)
        {
            _config = config;
        }

        public string GerarToken(Usuario usuario)
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, ConversorEnumSnakeCase<PapelUsuario>.ParaTexto(usuario.Papel)),
                new Claim(ClaimEmpresaId, usuario.EmpresaId.ToString())
            };

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
