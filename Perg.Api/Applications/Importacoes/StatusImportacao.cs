namespace Perg.Api.Applications.Importacoes
{
    // Valores do CHECK de importacoes.status (a coluna é texto)
    public static class StatusImportacao
    {
        public const string Enviada = "enviada";
        public const string Mapeando = "mapeando";
        public const string AguardandoConfirmacao = "aguardando_confirmacao";
        public const string Processando = "processando";
        public const string Concluida = "concluida";
        public const string Falhou = "falhou";
    }
}
