using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Sms
{
    // Sem provedor de SMS configurado: a mensagem aparece no Output do Visual Studio (como o e-mail sem Gmail)
    public class SmsConsole : IEnvioSms
    {
        private readonly ILogger<SmsConsole> _logger;

        public SmsConsole(ILogger<SmsConsole> logger)
        {
            _logger = logger;
        }

        public bool Enviar(string telefone, string mensagem)
        {
            _logger.LogWarning("SMS SIMULADO para +55{Telefone}: {Mensagem}", telefone, mensagem);
            return true;
        }
    }
}
