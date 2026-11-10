using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Email
{
    // SMTP com MailKit (testes: Gmail com "senha de app").
    // appsettings: Email:Host, Email:Porta (587 = STARTTLS, 465 = SSL direto), Email:NomeRemetente.
    // Segredos do Usuário: Email:Usuario, Email:Senha.
    public class SmtpEnvioEmail : IEnvioEmail
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEnvioEmail> _logger;

        public SmtpEnvioEmail(IConfiguration config, ILogger<SmtpEnvioEmail> logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool Enviar(string para, string assunto, string html)
        {
            string usuario = _config["Email:Usuario"]!;
            string host = _config["Email:Host"] ?? "smtp.gmail.com";
            int porta = _config.GetValue("Email:Porta", 587);

            try
            {
                var mensagem = new MimeMessage();
                mensagem.From.Add(new MailboxAddress(_config["Email:NomeRemetente"] ?? "Perg", usuario));
                mensagem.To.Add(MailboxAddress.Parse(para));
                mensagem.Subject = assunto;
                mensagem.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

                using var cliente = new SmtpClient { Timeout = 15000 };
                cliente.Connect(host, porta, porta == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                cliente.Authenticate(usuario, (_config["Email:Senha"] ?? string.Empty).Replace(" ", ""));
                cliente.Send(mensagem);
                cliente.Disconnect(true);
                return true;
            }
            catch (Exception ex)
            {
                // Erro de e-mail nunca derruba a requisição: o chamador decide o que fazer com o "false"
                _logger.LogError(ex, "Falha ao enviar e-mail \"{Assunto}\" para {Para} via {Host}:{Porta}.", assunto, para, host, porta);
                return false;
            }
        }
    }
}
