namespace Perg.Api.Applications.Arquivos
{
    // Organização das pastas. Tudo que começa com "publico/" pode ser aberto sem login (fotos de perfil);
    // o resto só pelos endpoints que conferem a permissão.
    public static class PastasArquivos
    {
        public const string PrefixoPublico = "publico/";

        public static string Comprovantes(Guid entregadorId) => $"comprovantes/{entregadorId}";
        public static string Ocorrencias(Guid entregadorId) => $"ocorrencias/{entregadorId}";
        public static string Documentos(Guid entregadorId) => $"documentos/{entregadorId}";
        public static string FotoLoja(Guid empresaId) => $"{PrefixoPublico}lojas/{empresaId}";
        public static string FotoUsuario(Guid usuarioId) => $"{PrefixoPublico}usuarios/{usuarioId}";
        public static string FotoEntregador(Guid entregadorId) => $"{PrefixoPublico}entregadores/{entregadorId}";

        // Endereço para o front montar a imagem
        public static string? UrlPublica(string? chave)
        {
            return chave != null && chave.StartsWith(PrefixoPublico) ? $"/api/Arquivos/{chave}" : null;
        }
    }
}
