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

        [HttpGet]
        public ActionResult Verificar()
        {
            bool banco = _context.Database.CanConnect();
            return banco ? Ok(new { status = "ok", banco }) : StatusCode(503, new { status = "indisponivel", banco });
        }
    }
}
