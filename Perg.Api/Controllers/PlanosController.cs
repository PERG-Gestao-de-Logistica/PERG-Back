using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.AssinaturasDto;

namespace Perg.Api.Controllers
{
    // Tabela de preços (aberta: usada também na página pública de planos)
    [Route("api/[controller]")]
    [ApiController]
    public class PlanosController : ControllerBase
    {
        private readonly AssinaturaService _service;

        public PlanosController(AssinaturaService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<List<LerPlanoDto>> Listar()
        {
            return Ok(_service.ListarPlanos());
        }
    }
}
