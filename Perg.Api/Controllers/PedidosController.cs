using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Services;
using Perg.Api.DTOs;
using Perg.Api.DTOs.PedidosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly PedidoService _service;

        public PedidosController(PedidoService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<ListaPaginadaDto<LerPedidoResumoDto>> ListarPedidos([FromQuery] FiltroPedidosDto filtro)
        {
            try
            {
                return Ok(_service.Listar(filtro, User.ObterEmpresaId()));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LerPedidoDto> BuscarPedidoPorId(Guid id)
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

        [HttpPost]
        public ActionResult CadastrarPedido(CriarPedidoDto dto)
        {
            try
            {
                LerPedidoDto pedido = _service.Criar(dto, User.ObterEmpresaId(), User.ObterUsuarioId());
                return StatusCode(201, new { mensagem = "Pedido criado com sucesso!", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public ActionResult EditarPedido(Guid id, AtualizarPedidoDto dto)
        {
            try
            {
                LerPedidoDto pedido = _service.Atualizar(id, dto, User.ObterUsuarioId());
                return Ok(new { mensagem = "Pedido atualizado com sucesso!", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/geocodificar")]
        public ActionResult GeocodificarPedido(Guid id)
        {
            try
            {
                LerPedidoDto pedido = _service.Geocodificar(id);
                return Ok(new { mensagem = "Geocodificação executada.", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPut("{id:guid}/coordenadas")]
        public ActionResult AjustarCoordenadas(Guid id, AjustarCoordenadasDto dto)
        {
            try
            {
                LerPedidoDto pedido = _service.AjustarCoordenadas(id, dto, User.ObterUsuarioId());
                return Ok(new { mensagem = "Localização ajustada.", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/cancelar")]
        public ActionResult CancelarPedido(Guid id, CancelarPedidoDto dto)
        {
            try
            {
                LerPedidoDto pedido = _service.Cancelar(id, dto, User.ObterUsuarioId());
                return Ok(new { mensagem = "Pedido cancelado.", pedido });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
