using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ArquivosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArquivosController : ControllerBase
    {
        private readonly ArquivoService _service;

        public ArquivosController(ArquivoService service)
        {
            _service = service;
        }

        // Fotos públicas (perfil, loja): /api/Arquivos/publico/lojas/{empresa}/{arquivo}.jpg
        [HttpGet("publico/{**caminho}")]
        [ResponseCache(Duration = 86400)]
        public ActionResult AbrirPublico(string caminho)
        {
            try
            {
                ArquivoParaDownload arquivo = _service.AbrirPublico("publico/" + caminho);
                return File(arquivo.Conteudo, arquivo.TipoConteudo);
            }
            catch (DomainException)
            {
                return NotFound();
            }
        }

        [HttpPut("foto-loja")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public ActionResult AtualizarFotoLoja([FromForm] EnviarArquivoDto dto)
        {
            try
            {
                return Ok(_service.AtualizarFotoLoja(User.ObterEmpresaId(), dto.Arquivo));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPut("foto-usuario")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public ActionResult AtualizarFotoUsuario([FromForm] EnviarArquivoDto dto)
        {
            try
            {
                return Ok(_service.AtualizarFotoUsuario(User.ObterUsuarioId(), dto.Arquivo));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Foto/assinatura do comprovante de entrega de um pedido da loja
        [HttpGet("pedidos/{pedidoId:guid}/comprovante")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AbrirComprovante(Guid pedidoId)
        {
            try
            {
                ArquivoParaDownload arquivo = _service.AbrirComprovante(pedidoId);
                return File(arquivo.Conteudo, arquivo.TipoConteudo);
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpGet("ocorrencias/{ocorrenciaId:guid}/foto")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AbrirFotoOcorrencia(Guid ocorrenciaId)
        {
            try
            {
                ArquivoParaDownload arquivo = _service.AbrirFotoOcorrencia(ocorrenciaId);
                return File(arquivo.Conteudo, arquivo.TipoConteudo);
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }
    }
}
