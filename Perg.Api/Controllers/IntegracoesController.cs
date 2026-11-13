using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.IntegracoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Portal da loja: chaves de API e webhooks da integração (só admin)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class IntegracoesController : ControllerBase
    {
        private readonly IntegracaoService _service;

        public IntegracoesController(IntegracaoService service)
        {
            _service = service;
        }

        [HttpGet("chaves")]
        public ActionResult ListarChaves() => Executar(() => Ok(_service.ListarChaves(User.ObterUsuarioId())));

        // A chave completa aparece só nesta resposta: guarde-a no sistema de vendas
        [HttpPost("chaves")]
        public ActionResult CriarChave(CriarChaveApiDto dto) => Executar(() =>
        {
            var (chave, chaveCompleta) = _service.CriarChave(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
            return StatusCode(201, new { mensagem = "Chave criada. Copie agora: ela não será mostrada de novo.", chave, chaveCompleta });
        });

        [HttpDelete("chaves/{id:guid}")]
        public ActionResult RevogarChave(Guid id) => Executar(() =>
        {
            _service.RevogarChave(id, User.ObterUsuarioId());
            return Ok(new { mensagem = "Chave revogada. Chamadas com ela passam a ser recusadas." });
        });

        [HttpGet("webhooks")]
        public ActionResult ListarWebhooks() => Executar(() => Ok(_service.ListarWebhooks(User.ObterUsuarioId())));

        // O segredo (para conferir a assinatura) aparece só nesta resposta
        [HttpPost("webhooks")]
        public ActionResult CriarWebhook(SalvarWebhookDto dto) => Executar(() =>
            StatusCode(201, new { mensagem = "Webhook criado. Guarde o segredo para validar a assinatura.", webhook = _service.CriarWebhook(dto, User.ObterEmpresaId(), User.ObterUsuarioId()) }));

        [HttpPut("webhooks/{id:guid}")]
        public ActionResult AtualizarWebhook(Guid id, SalvarWebhookDto dto) => Executar(() =>
            Ok(new { mensagem = "Webhook atualizado.", webhook = _service.AtualizarWebhook(id, dto, User.ObterUsuarioId()) }));

        [HttpDelete("webhooks/{id:guid}")]
        public ActionResult RemoverWebhook(Guid id) => Executar(() =>
        {
            _service.RemoverWebhook(id, User.ObterUsuarioId());
            return Ok(new { mensagem = "Webhook removido." });
        });

        [HttpPost("webhooks/{id:guid}/testar")]
        public ActionResult TestarWebhook(Guid id) => Executar(() =>
        {
            _service.TestarWebhook(id, User.ObterUsuarioId());
            return Ok(new { mensagem = "Evento \"ping\" enviado. Veja o resultado em entregas em alguns segundos." });
        });

        [HttpGet("webhooks/{id:guid}/entregas")]
        public ActionResult ListarEntregas(Guid id) => Executar(() => Ok(_service.ListarEntregas(id, User.ObterUsuarioId())));

        private ActionResult Executar(Func<ActionResult> acao)
        {
            try
            {
                return acao();
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
