using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.EntregadoresDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Visão da loja sobre a rede de entregadores e seus favoritos
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class EntregadoresController : ControllerBase
    {
        private readonly EntregadorService _service;

        public EntregadoresController(EntregadorService service)
        {
            _service = service;
        }

        // /api/Entregadores?tipoVeiculo=Moto&somenteFavoritos=true
        [HttpGet]
        public ActionResult<List<LerEntregadorRedeDto>> ListarRede([FromQuery] TipoVeiculo? tipoVeiculo, [FromQuery] bool somenteFavoritos = false)
        {
            return Ok(_service.ListarRede(tipoVeiculo, somenteFavoritos));
        }

        [HttpPost("{id:guid}/favorito")]
        public ActionResult Favoritar(Guid id)
        {
            try
            {
                _service.Favoritar(id, User.ObterEmpresaId());
                return Ok(new { mensagem = "Entregador adicionado aos favoritos." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpDelete("{id:guid}/favorito")]
        public ActionResult Desfavoritar(Guid id)
        {
            _service.Desfavoritar(id);
            return Ok(new { mensagem = "Entregador removido dos favoritos." });
        }
    }
}
