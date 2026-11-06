using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs;
using Perg.Api.DTOs.NotificacoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Sino do topo do painel da loja
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class NotificacoesController : ControllerBase
    {
        private readonly NotificacaoService _service;

        public NotificacoesController(NotificacaoService service)
        {
            _service = service;
        }

        // /api/Notificacoes?somenteNaoLidas=true&pagina=1&tamanhoPagina=20
        [HttpGet]
        public ActionResult<ListaPaginadaDto<LerNotificacaoDto>> ListarNotificacoes(
            [FromQuery] bool somenteNaoLidas = false, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
        {
            return Ok(_service.Listar(User.ObterUsuarioId(), somenteNaoLidas, pagina, tamanhoPagina));
        }

        // Número da bolinha do sino (o front pode consultar a cada 30 s)
        [HttpGet("contagem")]
        public ActionResult<ContagemNotificacoesDto> ContarNaoLidas()
        {
            return Ok(_service.Contar(User.ObterUsuarioId()));
        }

        [HttpPost("{id:guid}/lida")]
        public ActionResult MarcarComoLida(Guid id)
        {
            try
            {
                return Ok(_service.MarcarComoLida(id, User.ObterUsuarioId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpPost("lidas")]
        public ActionResult MarcarTodasComoLidas()
        {
            int quantidade = _service.MarcarTodasComoLidas(User.ObterUsuarioId());
            return Ok(new { mensagem = "Notificações marcadas como lidas.", quantidade });
        }

        [HttpGet("preferencias")]
        public ActionResult<PreferenciasNotificacaoDto> ObterPreferencias()
        {
            return Ok(_service.ObterPreferencias(User.ObterUsuarioId()));
        }

        [HttpPut("preferencias")]
        public ActionResult AtualizarPreferencias(PreferenciasNotificacaoDto dto)
        {
            PreferenciasNotificacaoDto preferencias = _service.AtualizarPreferencias(User.ObterUsuarioId(), dto);
            return Ok(new { mensagem = "Preferências salvas.", preferencias });
        }
    }
}
