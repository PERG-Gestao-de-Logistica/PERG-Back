using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Comum
{
    // Gravar e depois mandar em tempo real (NotificadorTempoReal.EnviarNotificacoes) continua com quem chama.
    public static class FabricaNotificacoes
    {
        private const int TamanhoMaximoTitulo = 200;

        public static List<Notificacao> Para(IEnumerable<Guid> usuarios, Guid empresaId, TipoNotificacao tipo, string titulo, string? link)
        {
            string tituloAjustado = titulo.Length > TamanhoMaximoTitulo ? titulo[..TamanhoMaximoTitulo] : titulo;

            return usuarios.Distinct().Select(usuarioId => new Notificacao
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                Tipo = tipo,
                Titulo = tituloAjustado,
                Link = link,
                CriadoEm = DateTime.UtcNow
            }).ToList();
        }
    }
}
