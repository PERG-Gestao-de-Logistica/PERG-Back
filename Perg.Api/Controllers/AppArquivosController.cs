using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ArquivosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Envio de fotos e documentos pelo app do entregador
    [Route("api/app")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Entregador)]
    public class AppArquivosController : ControllerBase
    {
        private readonly ArquivoService _service;

        public AppArquivosController(ArquivoService service)
        {
            _service = service;
        }

        // /api/app/arquivos?finalidade=Comprovante (ou Ocorrencia) — devolve a chave para usar na entrega
        [HttpPost("arquivos")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public ActionResult EnviarArquivo([FromQuery] FinalidadeArquivoApp finalidade, [FromForm] EnviarArquivoDto dto)
        {
            try
            {
                return Ok(_service.EnviarArquivoApp(User.ObterEntregadorId(), finalidade, dto.Arquivo));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPut("entregadores/eu/foto")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public ActionResult AtualizarFoto([FromForm] EnviarArquivoDto dto)
        {
            try
            {
                return Ok(_service.AtualizarFotoEntregador(User.ObterEntregadorId(), dto.Arquivo));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("documentos")]
        public ActionResult<List<LerDocumentoDto>> ListarDocumentos()
        {
            return Ok(_service.ListarDocumentos(User.ObterEntregadorId()));
        }

        [HttpPost("documentos")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public ActionResult EnviarDocumento([FromForm] EnviarDocumentoDto dto)
        {
            try
            {
                LerDocumentoDto documento = _service.EnviarDocumento(User.ObterEntregadorId(), dto);
                return StatusCode(201, new { mensagem = "Documento enviado para análise.", documento });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
