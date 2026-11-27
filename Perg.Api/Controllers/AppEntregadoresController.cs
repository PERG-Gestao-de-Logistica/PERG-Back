using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EntregadoresDto;
using Perg.Api.DTOs.RecuperacaoSenhaDto;
using Perg.Api.Exceptions;
using Microsoft.AspNetCore.RateLimiting;

namespace Perg.Api.Controllers
{
    [Route("api/app/entregadores")]
    [ApiController]
    public class AppEntregadoresController : ControllerBase
    {
        private readonly EntregadorService _service;
        private readonly RecuperacaoSenhaService _recuperacao;
        private readonly ConfirmacaoTelefoneService _confirmacaoTelefone;

        public AppEntregadoresController(EntregadorService service, RecuperacaoSenhaService recuperacao,
            ConfirmacaoTelefoneService confirmacaoTelefone)
        {
            _service = service;
            _recuperacao = recuperacao;
            _confirmacaoTelefone = confirmacaoTelefone;
        }

        [HttpPost("cadastro")]
        public ActionResult Cadastrar(CadastrarEntregadorDto dto)
        {
            try
            {
                LerEntregadorDto entregador = _service.Cadastrar(dto);
                return StatusCode(201, new { mensagem = "Cadastro enviado! Ele passa por análise antes de liberar o acesso.", entregador });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting(LimitesTentativas.Acesso)]
        public ActionResult Login(LoginEntregadorDto dto)
        {
            try
            {
                string token = _service.Login(dto);
                return Ok(new { mensagem = "Login realizado com sucesso!", token });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { mensagem = ex.Message });
            }
        }

        [HttpPost("esqueci-senha")]
        [EnableRateLimiting(LimitesTentativas.Acesso)]
        public ActionResult EsqueciSenha(EsqueciSenhaEntregadorDto dto)
        {
            _recuperacao.SolicitarEntregador(dto);
            return Ok(new { mensagem = "Se houver cadastro, enviamos um código para o e-mail cadastrado." });
        }

        [HttpPost("redefinir-senha")]
        [EnableRateLimiting(LimitesTentativas.Acesso)]
        public ActionResult RedefinirSenha(RedefinirSenhaEntregadorDto dto)
        {
            try
            {
                _recuperacao.RedefinirEntregador(dto);
                return Ok(new { mensagem = "Senha redefinida! Entre com a nova senha." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("eu")]
        [Authorize(Policy = PoliticasAcesso.Entregador)]
        public ActionResult<LerEntregadorDto> ObterPerfil()
        {
            try
            {
                return Ok(_service.ObterPerfil(User.ObterEntregadorId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Tela "Meu perfil": dados, veículo, entregas, sucesso, nota e documentos numa chamada
        [HttpGet("eu/perfil")]
        [Authorize(Policy = PoliticasAcesso.Entregador)]
        public ActionResult<LerPerfilEntregadorDto> ObterPerfilCompleto()
        {
            try
            {
                return Ok(_service.ObterPerfilCompleto(User.ObterEntregadorId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Confirmação do celular (liberada também com o cadastro em análise)
        [HttpPost("eu/telefone/enviar-codigo")]
        [EnableRateLimiting(LimitesTentativas.Acesso)]
        [Authorize(Policy = PoliticasAcesso.Entregador)]
        public ActionResult EnviarCodigoTelefone()
        {
            try
            {
                string numero = _confirmacaoTelefone.EnviarCodigo(User.ObterEntregadorId());
                return Ok(new { mensagem = $"Enviamos um código por SMS para {numero}." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("eu/telefone/confirmar")]
        [EnableRateLimiting(LimitesTentativas.Acesso)]
        [Authorize(Policy = PoliticasAcesso.Entregador)]
        public ActionResult ConfirmarTelefone(ConfirmarTelefoneDto dto)
        {
            try
            {
                _confirmacaoTelefone.Confirmar(User.ObterEntregadorId(), dto.Codigo);
                return Ok(new { mensagem = "Celular confirmado!" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
