namespace Perg.Api.DTOs.AvaliacoesDto
{
    public class AvaliarRotaDto
    {
        public Guid RotaId { get; set; }
        public short Nota { get; set; }            // 1 a 5
        public string? Comentario { get; set; }
    }

    // Rota concluída que a loja ainda não avaliou
    public class LerRotaParaAvaliarDto
    {
        public Guid RotaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateOnly DataOperacao { get; set; }
        public DateTime? ConcluidaEm { get; set; }
        public Guid EntregadorId { get; set; }
        public string EntregadorNome { get; set; } = string.Empty;
        public int Entregas { get; set; }
    }

    public class LerAvaliacaoDto
    {
        public Guid Id { get; set; }
        public Guid RotaId { get; set; }
        public string CodigoRota { get; set; } = string.Empty;
        public DateOnly DataOperacao { get; set; }
        public short Nota { get; set; }
        public string? Comentario { get; set; }
        public string? EntregadorNome { get; set; }   // visão da loja
        public string? NomeLoja { get; set; }         // visão do entregador
        public DateTime CriadoEm { get; set; }
    }

    // Resumo que o entregador vê no app
    public class LerMinhasAvaliacoesDto
    {
        public decimal? NotaMedia { get; set; }
        public int Total { get; set; }
        public Dictionary<int, int> PorNota { get; set; } = new();   // 1..5 → quantidade
        public List<LerAvaliacaoDto> Ultimas { get; set; } = new();
    }
}
