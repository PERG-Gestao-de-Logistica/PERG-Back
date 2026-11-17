namespace Perg.Api.Interfaces
{
    // Envio de SMS: TwilioEnvioSms (com Twilio:AccountSid) ou SmsConsole (texto no Output).
    public interface IEnvioSms
    {
        // telefone só com dígitos (DDD + número). Devolve false se não enviou.
        bool Enviar(string telefone, string mensagem);
    }
}
