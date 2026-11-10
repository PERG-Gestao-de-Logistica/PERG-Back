namespace Perg.Api.Interfaces
{
    // Envio de e-mail. Hoje: SMTP (Gmail com senha de app) ou console (sem Email:Usuario configurado).
    public interface IEnvioEmail
    {
        // Devolve false se não conseguiu enviar (o erro fica no log)
        bool Enviar(string para, string assunto, string html);
    }
}
