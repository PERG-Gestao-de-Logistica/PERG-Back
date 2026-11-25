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
    [Authorize(Policy = PoliticasAcesso.Lojista)]
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

        [HttpPut("{id:guid}")]
        public ActionResult AtualizarOrigem(Guid id, CriarOrigemDto dto)
        {
            try
            {
                LerOrigemDto origem = _service.Atualizar(id, dto);
                return Ok(new { mensagem = "Origem atualizada.", origem });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/padrao")]
        public ActionResult DefinirPadrao(Guid id)
        {
            try
            {
                LerOrigemDto origem = _service.DefinirPadrao(id);
                return Ok(new { mensagem = "Origem padrão atualizada.", origem });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Endereço -> ponto no mapa, para o modal mostrar antes de salvar
        [HttpPost("localizar")]
        public ActionResult Localizar(LocalizarOrigemDto dto)
        {
            try
            {
                var local = _service.Localizar(dto);
                return Ok(new { status = local.Status, latitude = local.Latitude, longitude = local.Longitude });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
