using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.NotificacoesDto
{
    public class LerNotificacaoDto
    {
        public Guid Id { get; set; }
        public TipoNotificacao Tipo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Mensagem { get; set; }

        // Tela que abre ao clicar, ex.: /rotas/{id}
        public string? Link { get; set; }
        public bool Lida { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    public class ContagemNotificacoesDto
    {
        public int NaoLidas { get; set; }
    }

    public class PreferenciasNotificacaoDto
    {
        public bool NotifOcorrencias { get; set; }
        public bool NotifCobranca { get; set; }
    }
}
