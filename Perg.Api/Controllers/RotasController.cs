using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.OfertasDto;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class RotasController : ControllerBase
    {
        private readonly RotaService _service;
        private readonly OfertaService _ofertaService;

        public RotasController(RotaService service, OfertaService ofertaService)
        {
            _service = service;
            _ofertaService = ofertaService;
        }

        // Envia a rota para os entregadores (favoritos primeiro, depois a rede)
        [HttpPost("{id:guid}/ofertar")]
        public ActionResult OfertarRota(Guid id, OfertarRotaDto dto)
        {
            try
            {
                LerRotaDto rota = _ofertaService.Ofertar(id, dto, User.ObterEmpresaId());
                return Ok(new { mensagem = "Rota ofertada aos entregadores.", rota });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("{id:guid}/ofertas")]
        public ActionResult<List<LerOfertaLojaDto>> ListarOfertasDaRota(Guid id)
        {
            return Ok(_ofertaService.ListarDaRota(id));
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
