namespace Perg.Api.Interfaces
{
    public interface IEnvioEmail
    {
        // Devolve false se não conseguiu enviar (o erro fica no log)
        bool Enviar(string para, string assunto, string html);
    }
}
