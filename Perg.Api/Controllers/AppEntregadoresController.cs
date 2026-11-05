using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EntregadoresDto;
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

        public AppEntregadoresController(EntregadorService service, IWebHostEnvironment ambiente)
        {
            _service = service;
            _ambiente = ambiente;
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
