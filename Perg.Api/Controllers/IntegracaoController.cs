using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Integracoes;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.IntegracoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // API pública para sistemas de vendas (ERP, loja virtual, PDV). Autenticação: cabeçalho X-Api-Key.
    // Versionada em /v1: mudanças incompatíveis vão para uma /v2, sem quebrar quem já integrou.
    [Route("api/integracao/v1")]
    [ApiController]
    [Authorize(AuthenticationSchemes = AutenticacaoChaveApi.Esquema, Policy = PoliticasAcesso.Integracao)]
    public class IntegracaoController : ControllerBase
    {
        private readonly IntegracaoService _service;

        public IntegracaoController(IntegracaoService service)
        {
            _service = service;
        }

        // Cria o pedido. 201 = criado; 200 = esse idExterno já existia (devolve o mesmo pedido, sem duplicar).
        [HttpPost("pedidos")]
        public ActionResult CriarPedido(CriarPedidoIntegracaoDto dto)
        {
            try
            {
                var (pedido, criado) = _service.CriarPedido(dto, User.ObterEmpresaId());
                return criado
                    ? StatusCode(201, new { mensagem = "Pedido criado.", pedido })
                    : Ok(new { mensagem = "Esse idExterno já tinha sido enviado. Nada foi duplicado.", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("pedidos/{idExterno}")]
        public ActionResult<LerPedidoIntegracaoDto> ObterPedido(string idExterno)
        {
            try
            {
                return Ok(_service.ObterPedido(idExterno));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Alternativa ao webhook: pedidos que mudaram depois de "desde" (ISO 8601), até 200 por vez, do mais antigo ao mais novo
        [HttpGet("pedidos")]
        public ActionResult<List<LerPedidoIntegracaoDto>> ListarAtualizados([FromQuery] DateTimeOffset desde)
        {
            return Ok(_service.ListarAtualizados(desde));
        }

        [HttpPost("pedidos/{idExterno}/cancelar")]
        public ActionResult CancelarPedido(string idExterno, CancelarPedidoIntegracaoDto dto)
        {
            try
            {
                return Ok(new { mensagem = "Pedido cancelado.", pedido = _service.CancelarPedido(idExterno, dto) });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
