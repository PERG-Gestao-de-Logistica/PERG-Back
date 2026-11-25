using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.PainelDto;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class PainelController : ControllerBase
    {
        private readonly PainelService _service;

        public PainelController(PainelService service)
        {
            _service = service;
        }

        // Visão geral: indicadores do dia, primeiros passos, rotas de hoje e ocorrências abertas
        [HttpGet("resumo")]
        public ActionResult<ResumoPainelDto> ObterResumo()
        {
            return Ok(_service.ObterResumo(User.ObterEmpresaId()));
        }
    }
}
