using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.OfertasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Ofertas de rota recebidas pelo entregador no app
    [Route("api/app/ofertas")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppOfertasController : ControllerBase
    {
        private readonly OfertaService _service;

        public AppOfertasController(OfertaService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<List<LerOfertaDto>> ListarOfertas()
        {
            return Ok(_service.ListarDoEntregador(User.ObterEntregadorId()));
        }

        [HttpPost("{id:guid}/aceitar")]
        public ActionResult Aceitar(Guid id)
        {
            try
            {
                _service.Aceitar(id, User.ObterEntregadorId());
                return Ok(new { mensagem = "Rota aceita! Vá até a loja no horário de coleta." });
            }
            catch (DomainException ex)
            {
                return Conflict(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/recusar")]
        public ActionResult Recusar(Guid id)
        {
            try
            {
                _service.Recusar(id, User.ObterEntregadorId());
                return Ok(new { mensagem = "Oferta recusada." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
