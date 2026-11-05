using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.OrigensDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrigensController : ControllerBase
    {
        private readonly OrigemService _service;

        public OrigensController(OrigemService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<List<LerOrigemDto>> ListarOrigens()
        {
            return Ok(_service.Listar());
        }

        [HttpPost]
        public ActionResult CadastrarOrigem(CriarOrigemDto dto)
        {
            try
            {
                LerOrigemDto origem = _service.Criar(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
                return StatusCode(201, new { mensagem = "Origem cadastrada com sucesso!", origem });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
