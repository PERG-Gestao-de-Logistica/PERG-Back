using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.BackofficeDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Painel interno do Perg (equipe Perg). Token de loja ou de entregador não entra aqui.
    [Route("api/backoffice")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Backoffice)]
    public class BackofficeController : ControllerBase
    {
        private readonly BackofficeService _service;

        public BackofficeController(BackofficeService service)
        {
            _service = service;
        }

        // ---------- Acesso ----------

        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult Login(LoginAdministradorDto dto)
        {
            try
            {
                var (token, trocarSenha) = _service.Login(dto);
                return Ok(new { mensagem = "Login realizado com sucesso!", token, trocarSenha });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new { mensagem = ex.Message });
            }
        }

        [HttpPut("eu/senha")]
        public ActionResult TrocarSenha(TrocarSenhaAdministradorDto dto)
        {
            return Executar(() =>
            {
                _service.TrocarSenha(User.ObterAdministradorId(), dto);
                return Ok(new { mensagem = "Senha alterada com sucesso!" });
            });
        }

        // ---------- Entregadores ----------

        // Fila de análise: ?status=EmAnalise (mais antigos primeiro). Busca por nome, e-mail, CPF ou celular.
        [HttpGet("entregadores")]
        public ActionResult<List<LerEntregadorResumoBackofficeDto>> ListarEntregadores([FromQuery] StatusEntregador? status, [FromQuery] string? busca)
        {
            return Ok(_service.ListarEntregadores(status, busca));
        }

        [HttpGet("entregadores/{id:guid}")]
        public ActionResult ObterEntregador(Guid id)
        {
            return Executar(() => Ok(_service.ObterEntregador(id)), naoEncontrado: true);
        }

        [HttpPost("entregadores/{id:guid}/aprovar")]
        public ActionResult AprovarEntregador(Guid id)
        {
            return Executar(() => Ok(new { mensagem = "Cadastro aprovado. O entregador foi avisado por e-mail.", entregador = _service.AprovarEntregador(id, User.ObterAdministradorId()) }));
        }

        [HttpPost("entregadores/{id:guid}/reprovar")]
        public ActionResult ReprovarEntregador(Guid id, MotivoDto dto)
        {
            return Executar(() => Ok(new { mensagem = "Cadastro reprovado. O entregador recebeu o motivo por e-mail.", entregador = _service.ReprovarEntregador(id, dto) }));
        }

        [HttpPost("entregadores/{id:guid}/suspender")]
        public ActionResult SuspenderEntregador(Guid id, MotivoDto dto)
        {
            return Executar(() => Ok(new { mensagem = "Entregador suspenso.", entregador = _service.SuspenderEntregador(id, dto) }));
        }

        [HttpPost("entregadores/{id:guid}/reativar")]
        public ActionResult ReativarEntregador(Guid id)
        {
            return Executar(() => Ok(new { mensagem = "Entregador reativado.", entregador = _service.ReativarEntregador(id) }));
        }

        // ---------- Documentos ----------

        [HttpGet("documentos/{id:guid}/arquivo")]
        public ActionResult BaixarArquivo(Guid id)
        {
            return Executar(() =>
            {
                var (conteudo, tipo) = _service.AbrirArquivoDocumento(id);
                return File(conteudo, tipo);
            }, naoEncontrado: true);
        }

        [HttpPost("documentos/{id:guid}/aprovar")]
        public ActionResult AprovarDocumento(Guid id)
        {
            return Executar(() => Ok(new { mensagem = "Documento aprovado.", documento = _service.AprovarDocumento(id, User.ObterAdministradorId()) }));
        }

        [HttpPost("documentos/{id:guid}/rejeitar")]
        public ActionResult RejeitarDocumento(Guid id, MotivoDto dto)
        {
            return Executar(() => Ok(new { mensagem = "Documento rejeitado. O entregador recebeu o motivo por e-mail.", documento = _service.RejeitarDocumento(id, dto, User.ObterAdministradorId()) }));
        }

        // ---------- Administradores ----------

        [HttpGet("administradores")]
        public ActionResult<List<LerAdministradorDto>> ListarAdministradores()
        {
            return Ok(_service.ListarAdministradores());
        }

        [HttpPost("administradores")]
        public ActionResult CriarAdministrador(CriarAdministradorDto dto)
        {
            return Executar(() =>
            {
                var (administrador, emailEnviado, senhaTemporaria) = _service.CriarAdministrador(dto);
                return StatusCode(201, new
                {
                    mensagem = emailEnviado
                        ? "Administrador criado. A senha provisória foi enviada por e-mail."
                        : "Administrador criado, mas o e-mail não foi enviado. Passe a senha provisória para ele.",
                    administrador,
                    emailEnviado,
                    senhaTemporaria
                });
            });
        }

        [HttpPatch("administradores/{id:guid}/desativar")]
        public ActionResult DesativarAdministrador(Guid id)
        {
            return Executar(() => Ok(new { mensagem = "Administrador desativado.", administrador = _service.DefinirAtivoAdministrador(id, false, User.ObterAdministradorId()) }));
        }

        [HttpPatch("administradores/{id:guid}/reativar")]
        public ActionResult ReativarAdministrador(Guid id)
        {
            return Executar(() => Ok(new { mensagem = "Administrador reativado.", administrador = _service.DefinirAtivoAdministrador(id, true, User.ObterAdministradorId()) }));
        }

        private ActionResult Executar(Func<ActionResult> acao, bool naoEncontrado = false)
        {
            try
            {
                return acao();
            }
            catch (DomainException ex)
            {
                return naoEncontrado ? NotFound(new { mensagem = ex.Message }) : BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
