namespace Perg.Api.Applications.Assinaturas
{
    // assinaturas.status é smallint: 1 Ativa, 2 Inadimplente, 3 Cancelada
    public static class StatusAssinatura
    {
        public const short Ativa = 1;
        public const short Inadimplente = 2;
        public const short Cancelada = 3;

        public static string Texto(short status) => status switch
        {
            Ativa => "Ativa",
            Inadimplente => "Inadimplente",
            Cancelada => "Cancelada",
            _ => "Desconhecido"
        };
    }
}
