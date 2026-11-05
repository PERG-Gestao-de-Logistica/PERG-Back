using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.RoteirizacoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class RoteirizacoesController : ControllerBase
    {
        private readonly RoteirizacaoService _service;

        public RoteirizacoesController(RoteirizacaoService service)
        {
            _service = service;
        }

        // Monta as rotas do dia (pode levar alguns segundos)
        [HttpPost]
        public ActionResult Roteirizar(CriarRoteirizacaoDto dto)
        {
            try
            {
                LerRoteirizacaoDto roteirizacao = _service.Criar(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
                return StatusCode(201, new { mensagem = "Roteirização concluída.", roteirizacao });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LerRoteirizacaoDto> BuscarRoteirizacaoPorId(Guid id)
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
    }
}
