using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EntregadoresDto;
using Perg.Api.DTOs.RecuperacaoSenhaDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Rotas usadas pelo app do entregador
    [Route("api/app/entregadores")]
    [ApiController]
    public class AppEntregadoresController : ControllerBase
    {
        private readonly EntregadorService _service;
        private readonly IWebHostEnvironment _ambiente;
        private readonly RecuperacaoSenhaService _recuperacao;

        public AppEntregadoresController(EntregadorService service, IWebHostEnvironment ambiente, RecuperacaoSenhaService recuperacao)
        {
            _service = service;
            _ambiente = ambiente;
            _recuperacao = recuperacao;
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

        // Esqueci a senha: login = CPF ou celular; o código vai para o e-mail cadastrado
        [HttpPost("esqueci-senha")]
        public ActionResult EsqueciSenha(EsqueciSenhaEntregadorDto dto)
        {
            _recuperacao.SolicitarEntregador(dto);
            return Ok(new { mensagem = "Se houver cadastro, enviamos um código para o e-mail cadastrado." });
        }

        [HttpPost("redefinir-senha")]
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

        // SÓ EM DESENVOLVIMENTO: aprova o cadastro sem SMS nem análise de documentos, para testar as ofertas
        [HttpPost("{id:guid}/aprovar-teste")]
        public ActionResult AprovarParaTeste(Guid id)
        {
            if (!_ambiente.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                LerEntregadorDto entregador = _service.AprovarParaTeste(id);
                return Ok(new { mensagem = "Cadastro aprovado (ambiente de desenvolvimento).", entregador });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
