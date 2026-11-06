using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Cada entrega da rota
    [Route("api/app/paradas")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppParadasController : ControllerBase
    {
        private readonly ExecucaoService _service;

        public AppParadasController(ExecucaoService service)
        {
            _service = service;
        }

        [HttpPost("{id:guid}/a-caminho")]
        public ActionResult MarcarACaminho(Guid id)
        {
            try
            {
                _service.MarcarACaminho(id, User.ObterEntregadorId());
                return Ok(new { mensagem = "Cliente avisado de que você está a caminho." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/entregar")]
        public ActionResult ConcluirEntrega(Guid id, ConcluirEntregaDto dto)
        {
            try
            {
                _service.ConcluirEntrega(id, dto, User.ObterEntregadorId());
                return Ok(new { mensagem = "Entrega confirmada!" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/ocorrencia")]
        public ActionResult RegistrarOcorrencia(Guid id, RegistrarOcorrenciaDto dto)
        {
            try
            {
                _service.RegistrarOcorrencia(id, dto, User.ObterEntregadorId());
                return Ok(new { mensagem = "Ocorrência registrada. A loja foi avisada." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
