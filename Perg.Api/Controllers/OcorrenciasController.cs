using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.OcorrenciasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class OcorrenciasController : ControllerBase
    {
        private readonly OcorrenciaService _service;

        public OcorrenciasController(OcorrenciaService service)
        {
            _service = service;
        }

        // /api/Ocorrencias?abertas=true
        [HttpGet]
        public ActionResult<List<LerOcorrenciaDto>> ListarOcorrencias([FromQuery] bool abertas = true)
        {
            return Ok(_service.Listar(abertas));
        }

        // Reentregar (o pedido volta para "novo") ou cancelar
        [HttpPost("{id:guid}/resolver")]
        public ActionResult ResolverOcorrencia(Guid id, ResolverOcorrenciaDto dto)
        {
            try
            {
                LerOcorrenciaDto ocorrencia = _service.Resolver(id, dto, User.ObterUsuarioId());
                return Ok(new { mensagem = "Ocorrência resolvida.", ocorrencia });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
