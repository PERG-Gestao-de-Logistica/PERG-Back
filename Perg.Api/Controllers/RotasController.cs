using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RotasController : ControllerBase
    {
        private readonly RotaService _service;

        public RotasController(RotaService service)
        {
            _service = service;
        }

        // /api/Rotas?data=2026-11-05
        [HttpGet]
        public ActionResult<List<LerRotaDto>> ListarRotasDoDia([FromQuery] DateOnly data)
        {
            return Ok(_service.ListarPorData(data));
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LerRotaDto> BuscarRotaPorId(Guid id)
        {
            try
            {
                return Ok(_service.ObterPorId(id));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/cancelar")]
        public ActionResult CancelarRota(Guid id)
        {
            try
            {
                LerRotaDto rota = _service.Cancelar(id, User.ObterUsuarioId());
                return Ok(new { mensagem = "Rota cancelada. Os pedidos voltaram para novo.", rota });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
