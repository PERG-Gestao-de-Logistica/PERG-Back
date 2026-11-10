using System.Net;

namespace Perg.Api.Applications.Email
{
    // Textos dos e-mails. Tudo que vem do usuário passa por HtmlEncode.
    public static class ModelosEmail
    {
        public static (string Assunto, string Html) CodigoRedefinicao(string nome, string codigo)
        {
            return ("Seu código para redefinir a senha do Perg", Layout($"""
                <p>Olá, {Encode(PrimeiroNome(nome))}!</p>
                <p>Use o código abaixo para criar uma nova senha. Ele vale por 15 minutos.</p>
                <p style="font-size:28px;font-weight:bold;letter-spacing:6px;margin:24px 0">{codigo}</p>
                <p>Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.</p>
                """));
        }

        public static (string Assunto, string Html) BoasVindasEquipe(string nome, string nomeEmpresa, string email, string senhaTemporaria)
        {
            return ($"Você foi adicionado à equipe {nomeEmpresa} no Perg", Layout($"""
                <p>Olá, {Encode(PrimeiroNome(nome))}!</p>
                <p>Você agora faz parte da equipe <b>{Encode(nomeEmpresa)}</b> no Perg.</p>
                <p>E-mail: <b>{Encode(email)}</b><br>Senha provisória: <b style="font-size:18px">{Encode(senhaTemporaria)}</b></p>
                <p>No primeiro acesso você vai criar a sua própria senha.</p>
                """));
        }

        public static (string Assunto, string Html) SenhaRedefinidaPeloAdmin(string nome, string senhaTemporaria)
        {
            return ("Sua senha do Perg foi redefinida", Layout($"""
                <p>Olá, {Encode(PrimeiroNome(nome))}!</p>
                <p>O administrador da sua empresa redefiniu a sua senha.</p>
                <p>Senha provisória: <b style="font-size:18px">{Encode(senhaTemporaria)}</b></p>
                <p>No próximo acesso você vai criar uma nova senha.</p>
                """));
        }

        private static string Layout(string conteudo)
        {
            return $"""
                <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#222;font-size:15px;line-height:1.5">
                  <p style="font-size:22px;font-weight:bold;color:#1a56db;margin:0 0 16px">Perg</p>
                  {conteudo}
                  <p style="color:#888;font-size:12px;margin-top:32px">Este é um e-mail automático, não responda.</p>
                </div>
                """;
        }

        private static string PrimeiroNome(string nome) => nome.Trim().Split(' ')[0];

        private static string Encode(string texto) => WebUtility.HtmlEncode(texto);
    }
}
