using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EquipeDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    // Equipe da loja. Listar: qualquer usuário da loja. Alterar: só admin (403 para funcionário).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class EquipeController : ControllerBase
    {
        private readonly EquipeService _service;

        public EquipeController(EquipeService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<List<LerMembroDto>> Listar([FromQuery] bool incluirInativos = false)
        {
            return Ok(_service.Listar(incluirInativos));
        }

        [HttpPost]
        public ActionResult Criar(CriarMembroDto dto)
        {
            try
            {
                MembroComSenhaDto resultado = _service.Criar(dto, User.ObterUsuarioId());
                return StatusCode(201, new
                {
                    mensagem = resultado.EmailEnviado
                        ? "Membro cadastrado. A senha provisória foi enviada para o e-mail dele."
                        : "Membro cadastrado, mas o e-mail não foi enviado. Passe a senha provisória para ele: ela aparece só agora.",
                    membro = resultado.Membro,
                    emailEnviado = resultado.EmailEnviado,
                    senhaTemporaria = resultado.SenhaTemporaria
                });
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

        [HttpPut("{id}")]
        public ActionResult Atualizar(Guid id, AtualizarMembroDto dto)
        {
            try
            {
                LerMembroDto membro = _service.Atualizar(id, dto, User.ObterUsuarioId());
                return Ok(new { mensagem = "Membro atualizado.", membro });
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

        [HttpPatch("{id}/desativar")]
        public ActionResult Desativar(Guid id)
        {
            try
            {
                LerMembroDto membro = _service.Desativar(id, User.ObterUsuarioId());
                return Ok(new { mensagem = "Acesso desativado.", membro });
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

        [HttpPatch("{id}/reativar")]
        public ActionResult Reativar(Guid id)
        {
            try
            {
                LerMembroDto membro = _service.Reativar(id, User.ObterUsuarioId());
                return Ok(new { mensagem = "Acesso reativado.", membro });
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

        [HttpPost("{id}/redefinir-senha")]
        public ActionResult RedefinirSenha(Guid id)
        {
            try
            {
                MembroComSenhaDto resultado = _service.RedefinirSenha(id, User.ObterUsuarioId());
                return Ok(new
                {
                    mensagem = resultado.EmailEnviado
                        ? "Senha redefinida. A senha provisória foi enviada para o e-mail do membro."
                        : "Senha redefinida, mas o e-mail não foi enviado. Passe a senha provisória para o membro.",
                    membro = resultado.Membro,
                    emailEnviado = resultado.EmailEnviado,
                    senhaTemporaria = resultado.SenhaTemporaria
                });
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
