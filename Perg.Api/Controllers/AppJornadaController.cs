using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Chave "Fora do expediente / Iniciar jornada" do app
    [Route("api/app/jornada")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppJornadaController : ControllerBase
    {
        private readonly ExecucaoService _service;

        public AppJornadaController(ExecucaoService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<LerJornadaDto?> ObterJornada()
        {
            return Ok(_service.ObterJornada(User.ObterEntregadorId()));
        }

        [HttpPost("iniciar")]
        public ActionResult IniciarJornada()
        {
            try
            {
                LerJornadaDto jornada = _service.IniciarJornada(User.ObterEntregadorId());
                return Ok(new { mensagem = "Jornada iniciada. Bom trabalho!", jornada });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("encerrar")]
        public ActionResult EncerrarJornada()
        {
            try
            {
                LerJornadaDto jornada = _service.EncerrarJornada(User.ObterEntregadorId());
                return Ok(new { mensagem = "Jornada encerrada.", jornada });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
