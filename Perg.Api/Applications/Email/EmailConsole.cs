using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Email
{
    // Sem Email:Usuario configurado: o e-mail aparece no Output do Visual Studio e conta como NÃO enviado
    public class EmailConsole : IEnvioEmail
    {
        private readonly ILogger<EmailConsole> _logger;

        public EmailConsole(ILogger<EmailConsole> logger)
        {
            _logger = logger;
        }

        public bool Enviar(string para, string assunto, string html)
        {
            _logger.LogWarning("E-MAIL NÃO ENVIADO (Email:Usuario não configurado) → {Para} | {Assunto}\n{Html}", para, assunto, html);
            return false;
        }
    }
}
