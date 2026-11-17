using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.AvaliacoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class AvaliacoesController : ControllerBase
    {
        private readonly AvaliacaoService _service;

        public AvaliacoesController(AvaliacaoService service)
        {
            _service = service;
        }

        [HttpGet("pendentes")]
        public ActionResult<List<LerRotaParaAvaliarDto>> ListarPendentes()
        {
            return Ok(_service.ListarPendentes());
        }

        [HttpGet]
        public ActionResult<List<LerAvaliacaoDto>> Listar()
        {
            return Ok(_service.ListarDaEmpresa());
        }

        [HttpPost]
        public ActionResult Avaliar(AvaliarRotaDto dto)
        {
            try
            {
                LerAvaliacaoDto avaliacao = _service.Avaliar(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
                return StatusCode(201, new { mensagem = "Avaliação registrada. Obrigado!", avaliacao });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
