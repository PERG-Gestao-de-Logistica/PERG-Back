using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EmpresasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly EmpresaService _service;

        public EmpresasController(EmpresaService service)
        {
            _service = service;
        }

        [HttpPost("cadastrar")]
        public ActionResult Cadastrar(CadastrarEmpresaDto dto)
        {
            try
            {
                LerEmpresaDto empresa = _service.Cadastrar(dto);
                return StatusCode(201, new { mensagem = "Empresa cadastrada com sucesso!", empresa });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Dados da empresa do usuário logado (admin e funcionário podem ver)
        [HttpGet("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult<LerConfiguracaoEmpresaDto> ObterMinha()
        {
            try
            {
                return Ok(_service.ObterConfiguracao(User.ObterEmpresaId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Só o admin altera (403 para funcionário)
        [HttpPut("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AtualizarMinha(AtualizarEmpresaDto dto)
        {
            try
            {
                LerConfiguracaoEmpresaDto empresa = _service.AtualizarConfiguracao(User.ObterUsuarioId(), dto);
                return Ok(new { mensagem = "Dados da empresa atualizados.", empresa });
            }
            catch (AcessoNegadoException ex)
            {
                return StatusCode(403, new { mensagem = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
