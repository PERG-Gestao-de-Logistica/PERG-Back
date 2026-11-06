using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ExecucaoDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Saldo e extrato do entregador
    [Route("api/app/carteira")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppCarteiraController : ControllerBase
    {
        private readonly ExecucaoService _service;

        public AppCarteiraController(ExecucaoService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<LerCarteiraDto> ObterCarteira()
        {
            return Ok(_service.ObterCarteira(User.ObterEntregadorId()));
        }
    }
}
