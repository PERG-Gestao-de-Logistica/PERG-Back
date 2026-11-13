namespace Perg.Api.Applications.Autenticacao
{
    // Separa quem é usuário da loja de quem é entregador.
    // Lojista: token com empresa_id. Entregador: token com entregador_id.
    public static class PoliticasAcesso
    {
        public const string Lojista = "Lojista";
        public const string Entregador = "Entregador";

        // Equipe interna do Perg: token com administrador_id
        public const string Backoffice = "Backoffice";

        // Sistemas de vendas: chave de API (X-Api-Key) com chave_api_id
        public const string Integracao = "Integracao";
    }
}
