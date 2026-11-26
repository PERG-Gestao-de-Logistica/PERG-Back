using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs.EmpresasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly EmpresaService _service;
        private readonly TabelaValoresService _tabelaValores;

        public EmpresasController(EmpresaService service, TabelaValoresService tabelaValores)
        {
            _service = service;
            _tabelaValores = tabelaValores;
        }

        [HttpPost("cadastrar")]
        public ActionResult Cadastrar(CadastrarEmpresaDto dto)
        {
            try
            {
                LerEmpresaDto empresa = _service.Cadastrar(dto);
                return StatusCode(201, new { mensagem = "Empresa cadastrada com sucesso!", empresa });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Dados da empresa do usuário logado (admin e funcionário podem ver)
        [HttpGet("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult<LerConfiguracaoEmpresaDto> ObterMinha()
        {
            try
            {
                return Ok(_service.ObterConfiguracao(User.ObterEmpresaId()));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        // Só o admin altera (403 para funcionário)
        [HttpPut("eu")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AtualizarMinha(AtualizarEmpresaDto dto)
        {
            try
            {
                LerConfiguracaoEmpresaDto empresa = _service.AtualizarConfiguracao(User.ObterUsuarioId(), dto);
                return Ok(new { mensagem = "Dados da empresa atualizados.", empresa });
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

        // Valores pagos aos entregadores por tipo de veículo (tabela da loja ou a padrão)
        [HttpGet("eu/valores-entregador")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult<List<LerValorVeiculoDto>> ListarValoresEntregador()
        {
            return Ok(_tabelaValores.Listar());
        }

        // Só admin. Tipos que não vierem voltam para o padrão.
        [HttpPut("eu/valores-entregador")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult AtualizarValoresEntregador(AtualizarValoresVeiculoDto dto)
        {
            try
            {
                List<LerValorVeiculoDto> valores = _tabelaValores.Atualizar(User.ObterUsuarioId(), dto);
                return Ok(new { mensagem = "Valores dos entregadores salvos.", valores });
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

        // Só admin. Volta todos os tipos para a tabela padrão do Perg.
        [HttpDelete("eu/valores-entregador")]
        [Authorize(Policy = PoliticasAcesso.Lojista)]
        public ActionResult RestaurarValoresEntregador()
        {
            try
            {
                List<LerValorVeiculoDto> valores = _tabelaValores.RestaurarPadrao(User.ObterUsuarioId());
                return Ok(new { mensagem = "Valores voltaram para a tabela padrão.", valores });
            }
            catch (AcessoNegadoException ex)
            {
                return StatusCode(403, new { mensagem = ex.Message });
            }
        }
    }
}
