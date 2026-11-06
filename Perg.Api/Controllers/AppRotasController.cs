using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Rotas aceitas pelo entregador
    [Route("api/app/rotas")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppRotasController : ControllerBase
    {
        private readonly ExecucaoService _service;

        public AppRotasController(ExecucaoService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<List<LerRotaAppDto>> ListarMinhasRotas()
        {
            return Ok(_service.ListarRotas(User.ObterEntregadorId()));
        }

        // Na loja, depois de pegar os pacotes
        [HttpPost("{id:guid}/iniciar")]
        public ActionResult IniciarRota(Guid id)
        {
            try
            {
                LerRotaAppDto rota = _service.IniciarRota(id, User.ObterEntregadorId());
                return Ok(new { mensagem = "Rota iniciada. Os clientes foram avisados.", rota });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
