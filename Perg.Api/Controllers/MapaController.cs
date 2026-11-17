using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.PosicoesDto;

namespace Perg.Api.Controllers
{
    // Mapa da loja. Carga inicial por aqui; as atualizações chegam pelo SignalR (/hubs/rastreamento).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class MapaController : ControllerBase
    {
        private readonly PosicaoService _service;

        public MapaController(PosicaoService service)
        {
            _service = service;
        }

        [HttpGet("entregadores")]
        public ActionResult<List<LerPosicaoAtualDto>> ListarEntregadoresEmRota()
        {
            return Ok(_service.ListarAtuais(User.ObterEmpresaId()));
        }

        [HttpGet("rotas/{id:guid}/trilha")]
        public ActionResult<List<LerPontoTrilhaDto>> ListarTrilha(Guid id)
        {
            return Ok(_service.ListarTrilha(id));
        }
    }
}
