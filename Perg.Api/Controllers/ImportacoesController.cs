using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.ImportacoesDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Importação de pedidos por planilha:
    // POST (envia o arquivo) → GET {id} (colunas + sugestão) → POST {id}/confirmar → GET {id} (acompanha o progresso)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class ImportacoesController : ControllerBase
    {
        private readonly ImportacaoService _service;

        public ImportacoesController(ImportacaoService service)
        {
            _service = service;
        }

        [HttpPost]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public ActionResult Enviar([FromForm] EnviarPlanilhaDto dto)
        {
            try
            {
                LerImportacaoDto importacao = _service.Enviar(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
                return StatusCode(201, new { mensagem = "Planilha recebida. Confira as colunas e confirme a importação.", importacao });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult<List<LerImportacaoResumoDto>> Listar()
        {
            return Ok(_service.Listar());
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LerImportacaoDto> Obter(Guid id)
        {
            try
            {
                return Ok(_service.Obter(id));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/confirmar")]
        public ActionResult Confirmar(Guid id, ConfirmarImportacaoDto dto)
        {
            try
            {
                LerImportacaoDto importacao = _service.Confirmar(id, dto);
                return Ok(new { mensagem = "Importação iniciada. Os pedidos aparecem aos poucos; você recebe uma notificação no fim.", importacao });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Planilha modelo para a loja preencher (CSV com ; e acentos, abre direto no Excel)
        [HttpGet("modelo")]
        [AllowAnonymous]
        public ActionResult BaixarModelo()
        {
            const string conteudo =
                "Pedido;Cliente;Telefone;E-mail;CEP;Endereço;Número;Complemento;Bairro;Cidade;UF;Referência;Modalidade;Data;Hora início;Hora fim;Peso;Volumes;Observações\r\n" +
                "1001;Maria Silva;11987654321;maria@email.com;01310-100;Avenida Paulista;1578;Apto 12;Bela Vista;São Paulo;SP;Em frente ao MASP;Mesmo dia;;;;1,5;1;Interfone 12\r\n" +
                "1002;João Souza;11912345678;;04538-133;Avenida Brigadeiro Faria Lima;3477;;Itaim Bibi;São Paulo;SP;;Agendada;20/11/2026;14:00;16:00;3;2;\r\n";

            // BOM UTF-8: sem ele o Excel mostra os acentos errados
            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(conteudo)).ToArray();
            return File(bytes, "text/csv", "modelo-importacao-perg.csv");
        }
    }
}
