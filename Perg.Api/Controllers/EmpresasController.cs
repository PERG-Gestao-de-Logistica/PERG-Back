using Microsoft.AspNetCore.Mvc;
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

        // Onboarding: cria a empresa, o usuário admin e a origem padrão
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
    }
}
