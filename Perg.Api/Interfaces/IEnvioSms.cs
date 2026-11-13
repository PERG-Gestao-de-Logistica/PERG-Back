namespace Perg.Api.Interfaces
{
    // Envio de SMS. Hoje: simulado (o texto aparece no Output). Depois: Twilio, Zenvia ou WhatsApp,
    // trocando só a implementação registrada no Program.cs.
    public interface IEnvioSms
    {
        // telefone só com dígitos (DDD + número). Devolve false se não enviou.
        bool Enviar(string telefone, string mensagem);
    }
}
