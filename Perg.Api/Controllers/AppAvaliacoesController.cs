using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.AvaliacoesDto;

namespace Perg.Api.Controllers
{
    // App do entregador: nota média, quantidade por nota e últimas avaliações recebidas
    [Route("api/app/avaliacoes")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppAvaliacoesController : ControllerBase
    {
        private readonly AvaliacaoService _service;

        public AppAvaliacoesController(AvaliacaoService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<LerMinhasAvaliacoesDto> Obter()
        {
            return Ok(_service.MinhasAvaliacoes(User.ObterEntregadorId()));
        }
    }
}
