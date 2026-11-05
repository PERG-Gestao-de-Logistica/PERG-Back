using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.RastreioDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Público, sem login: é o link que o cliente final recebe
    [Route("api/[controller]")]
    [ApiController]
    public class RastreioController : ControllerBase
    {
        private readonly RastreioService _service;

        public RastreioController(RastreioService service)
        {
            _service = service;
        }

        [HttpGet("{token}")]
        public ActionResult<LerRastreioDto> ObterRastreio(string token)
        {
            try
            {
                return Ok(_service.Obter(token));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }
    }
}
