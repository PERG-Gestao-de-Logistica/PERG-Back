using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.SaquesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Botão "Sacar" da carteira do app
    [Route("api/app/carteira")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppSaquesController : ControllerBase
    {
        private readonly SaqueService _service;

        public AppSaquesController(SaqueService service)
        {
            _service = service;
        }

        [HttpPut("chave-pix")]
        public ActionResult AtualizarChavePix(AtualizarChavePixDto dto)
        {
            try
            {
                string chave = _service.AtualizarChavePix(User.ObterEntregadorId(), dto);
                return Ok(new { mensagem = "Chave Pix salva.", chavePix = chave });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("saques")]
        public ActionResult<List<LerSaqueDto>> ListarSaques()
        {
            return Ok(_service.Listar(User.ObterEntregadorId()));
        }

        [HttpPost("saques")]
        public ActionResult Sacar(SolicitarSaqueDto dto)
        {
            try
            {
                LerSaqueDto saque = _service.Solicitar(User.ObterEntregadorId(), dto);
                return Ok(new { mensagem = "Saque solicitado.", saque });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
