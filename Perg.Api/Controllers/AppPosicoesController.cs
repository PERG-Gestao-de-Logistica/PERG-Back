using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.PosicoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // GPS do app do entregador
    [Route("api/app/posicoes")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppPosicoesController : ControllerBase
    {
        private readonly PosicaoService _service;

        public AppPosicoesController(PosicaoService service)
        {
            _service = service;
        }

        [HttpPost]
        public ActionResult EnviarPosicoes(EnviarPosicoesDto dto)
        {
            try
            {
                int gravadas = _service.Registrar(User.ObterEntregadorId(), dto);
                return Ok(new { gravadas });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
