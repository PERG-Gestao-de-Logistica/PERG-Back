using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.AssinaturasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Plano da loja logada. Ver: qualquer usuário. Trocar/cancelar: só admin (403).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class AssinaturaController : ControllerBase
    {
        private readonly AssinaturaService _service;
        private readonly IWebHostEnvironment _ambiente;

        public AssinaturaController(AssinaturaService service, IWebHostEnvironment ambiente)
        {
            _service = service;
            _ambiente = ambiente;
        }

        [HttpGet]
        public ActionResult<LerAssinaturaDto> Obter()
        {
            try
            {
                return Ok(_service.Obter(User.ObterEmpresaId()));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Plano pago: a resposta traz o linkPagamento da primeira fatura (boleto, Pix ou cartão)
        [HttpPost("plano")]
        public ActionResult TrocarPlano(TrocarPlanoDto dto)
        {
            try
            {
                LerAssinaturaDto assinatura = _service.TrocarPlano(User.ObterEmpresaId(), User.ObterUsuarioId(), dto);
                string mensagem = assinatura.LinkPagamento != null
                    ? $"Plano alterado para {assinatura.Plano.Nome}. Pague a primeira fatura pelo link."
                    : $"Plano alterado para {assinatura.Plano.Nome}.";
                return Ok(new { mensagem, assinatura });
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

        [HttpPost("cancelar")]
        public ActionResult Cancelar()
        {
            try
            {
                LerAssinaturaDto assinatura = _service.Cancelar(User.ObterEmpresaId(), User.ObterUsuarioId());
                return Ok(new { mensagem = "Assinatura cancelada. A loja voltou para o plano grátis.", assinatura });
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

        // SÓ EM DESENVOLVIMENTO: marca a fatura em aberto como paga no Asaas sandbox
        [HttpPost("simular-pagamento")]
        public ActionResult SimularPagamento()
        {
            if (!_ambiente.IsDevelopment()) return NotFound();

            try
            {
                LerAssinaturaDto assinatura = _service.SimularPagamento(User.ObterEmpresaId());
                return Ok(new { mensagem = "Pagamento simulado (ambiente de desenvolvimento).", assinatura });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
