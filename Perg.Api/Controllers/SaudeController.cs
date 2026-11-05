using Microsoft.AspNetCore.Mvc;
using Perg.Api.Contexts;

namespace Perg.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaudeController : ControllerBase
    {
        private readonly PergDbContext _context;

        public SaudeController(PergDbContext context)
        {
            _context = context;
        }

        // Teste rápido da conexão com o banco
        [HttpGet("banco")]
        public ActionResult VerificarBanco()
        {
            bool conectado = _context.Database.CanConnect();

            return Ok(new
            {
                conectado,
                planos = _context.Planos.Count(),
                empresas = _context.Empresas.Count()
            });
        }
    }
}
