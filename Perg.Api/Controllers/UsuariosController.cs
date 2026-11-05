using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.UsuariosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _service;

        public UsuariosController(UsuarioService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public ActionResult Login(LoginUsuarioDto dto)
        {
            try
            {
                string tokenJwt = _service.Login(dto);
                return Ok(new { mensagem = "Login realizado com sucesso!", token = tokenJwt });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { mensagem = ex.Message });
            }
        }

        // Dados do usuário logado (serve para testar o token no Swagger)
        [HttpGet("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult<LerUsuarioDto> ObterUsuarioLogado()
        {
            try
            {
                Guid id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                LerUsuarioDto usuario = _service.ObterPerfil(id);
                return Ok(usuario);
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }
    }
}
