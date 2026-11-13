using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.RecuperacaoSenhaDto;
using Perg.Api.DTOs.UsuariosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _service;
        private readonly RecuperacaoSenhaService _recuperacao;
        private readonly IConfiguration _config;

        public UsuariosController(UsuarioService service, RecuperacaoSenhaService recuperacao, IConfiguration config)
        {
            _service = service;
            _recuperacao = recuperacao;
            _config = config;
        }

        [HttpPost("login")]
        public ActionResult Login(LoginUsuarioDto dto)
        {
            try
            {
                ResultadoLogin resultado = _service.Login(dto);
                return Ok(new { mensagem = "Login realizado com sucesso!", token = resultado.Token, trocarSenha = resultado.TrocarSenha });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { mensagem = ex.Message });
            }
        }

        // Entrar com Google: o front manda o "credential" (ID token) que o botão do Google devolve
        [HttpPost("login-google")]
        public ActionResult LoginGoogle(LoginGoogleDto dto)
        {
            try
            {
                ResultadoLogin resultado = _service.LoginGoogle(dto.IdToken);
                return Ok(new { mensagem = "Login realizado com sucesso!", token = resultado.Token, trocarSenha = resultado.TrocarSenha });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { mensagem = ex.Message });
            }
        }

        // Client ID público, para o front montar o botão do Google
        [HttpGet("login-google/config")]
        public ActionResult ConfigGoogle()
        {
            return Ok(new { clientId = _config["Google:ClientId"] });
        }

        [HttpPost("eu/google")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult VincularGoogle(LoginGoogleDto dto)
        {
            try
            {
                _service.VincularGoogle(User.ObterUsuarioId(), dto.IdToken);
                return Ok(new { mensagem = "Conta Google ligada. Agora dá para entrar com o Google." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpDelete("eu/google")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult DesvincularGoogle()
        {
            try
            {
                _service.DesvincularGoogle(User.ObterUsuarioId());
                return Ok(new { mensagem = "Conta Google desligada." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Esqueci a senha: manda um código de 6 dígitos para o e-mail (resposta igual exista ou não a conta)
        [HttpPost("esqueci-senha")]
        public ActionResult EsqueciSenha(EsqueciSenhaUsuarioDto dto)
        {
            _recuperacao.SolicitarUsuario(dto);
            return Ok(new { mensagem = "Se o e-mail estiver cadastrado, enviamos um código para redefinir a senha." });
        }

        [HttpPost("redefinir-senha")]
        public ActionResult RedefinirSenha(RedefinirSenhaUsuarioDto dto)
        {
            try
            {
                _recuperacao.RedefinirUsuario(dto);
                return Ok(new { mensagem = "Senha redefinida! Faça login com a nova senha." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Dados do usuário logado
        [HttpGet("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult<LerUsuarioDto> ObterUsuarioLogado()
        {
            try
            {
                return Ok(_service.ObterPerfil(User.ObterUsuarioId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Nome, telefone e preferências de notificação do próprio usuário
        [HttpPut("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AtualizarPerfil(AtualizarPerfilDto dto)
        {
            try
            {
                LerUsuarioDto usuario = _service.AtualizarPerfil(User.ObterUsuarioId(), dto);
                return Ok(new { mensagem = "Perfil atualizado.", usuario });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Troca da própria senha (também usada no primeiro acesso com a senha provisória)
        [HttpPut("eu/senha")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult TrocarSenha(TrocarSenhaDto dto)
        {
            try
            {
                _service.TrocarSenha(User.ObterUsuarioId(), dto);
                return Ok(new { mensagem = "Senha alterada com sucesso!" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
