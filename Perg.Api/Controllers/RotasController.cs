using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.Applications.Simulacao;
using Perg.Api.DTOs.OfertasDto;
using Perg.Api.DTOs.RotasDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PoliticasAcesso.Lojista)]
    public class RotasController : ControllerBase
    {
        private readonly RotaService _service;
        private readonly OfertaService _ofertaService;
        private readonly EstadoSimulacao _simulacao;
        private readonly IConfiguration _config;

        public RotasController(RotaService service, OfertaService ofertaService, EstadoSimulacao simulacao, IConfiguration config)
        {
            _service = service;
            _ofertaService = ofertaService;
            _simulacao = simulacao;
            _config = config;
        }

        // Simulação dos entregadores (só no servidor de demonstração: Demo:SimulacaoHabilitada)
        [HttpGet("simulacao")]
        public ActionResult ObterSimulacao()
        {
            bool habilitada = _config.GetValue("Demo:SimulacaoHabilitada", false);
            return Ok(new
            {
                habilitada,
                ativa = habilitada && _simulacao.EstaLigada(User.ObterEmpresaId()),
                fatorVelocidade = _config.GetValue("Demo:FatorVelocidade", 10.0)
            });
        }

        // Só o admin liga/desliga
        [HttpPost("simulacao")]
        public ActionResult AlterarSimulacao(AlterarSimulacaoDto dto)
        {
            if (!_config.GetValue("Demo:SimulacaoHabilitada", false))
            {
                return BadRequest(new { mensagem = "A simulação só está disponível no servidor de demonstração." });
            }
            bool admin = User.Claims.Any(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "admin");
            if (!admin)
            {
                return StatusCode(403, new { mensagem = "Só o administrador da loja pode ligar a simulação." });
            }

            Guid empresaId = User.ObterEmpresaId();
            if (dto.Ativa)
            {
                _simulacao.Ligar(empresaId);
                return Ok(new { mensagem = "Simulação ligada: os entregadores vão aceitar, iniciar e fazer as rotas.", ativa = true });
            }

            _simulacao.Empresas.TryRemove(empresaId, out _);
            return Ok(new { mensagem = "Simulação desligada. As rotas param onde estão.", ativa = false });
        }

        [HttpPost("{id:guid}/ofertar")]
        public ActionResult OfertarRota(Guid id, OfertarRotaDto dto)
        {
            try
            {
                LerRotaDto rota = _ofertaService.Ofertar(id, dto, User.ObterEmpresaId());
                return Ok(new { mensagem = "Rota ofertada aos entregadores.", rota });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("{id:guid}/ofertas")]
        public ActionResult<List<LerOfertaLojaDto>> ListarOfertasDaRota(Guid id)
        {
            return Ok(_ofertaService.ListarDaRota(id));
        }

        [HttpGet]
        public ActionResult<List<LerRotaDto>> ListarRotasDoDia([FromQuery] DateOnly data, [FromQuery] bool incluirAtivas = false)
        {
            return Ok(_service.ListarPorData(data, incluirAtivas));
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LerRotaDto> BuscarRotaPorId(Guid id)
        {
            try
            {
                return Ok(_service.ObterPorId(id));
            }
            catch (DomainException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/cancelar")]
        public ActionResult CancelarRota(Guid id)
        {
            try
            {
                LerRotaDto rota = _service.Cancelar(id, User.ObterUsuarioId());
                return Ok(new { mensagem = "Rota cancelada. Os pedidos voltaram para novo.", rota });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }

    public class AlterarSimulacaoDto
    {
        public bool Ativa { get; set; }
    }
}
