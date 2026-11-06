namespace Perg.Api.DTOs.PosicoesDto
{
    // O app junta as leituras do GPS e envia em lote (ex.: a cada 10-15 s)
    public class EnviarPosicoesDto
    {
        public List<PosicaoDto> Posicoes { get; set; } = new();
    }

    public class PosicaoDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        // Horário do GPS no celular
        public DateTimeOffset RegistradaEm { get; set; }
        public decimal? PrecisaoM { get; set; }
        public decimal? VelocidadeKmh { get; set; }
    }

    // Mapa da loja
    public class LerPosicaoAtualDto
    {
        public Guid EntregadorId { get; set; }
        public string EntregadorNome { get; set; } = string.Empty;
        public Guid? RotaId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? VelocidadeKmh { get; set; }
        public DateTime RegistradaEm { get; set; }
    }

    // Percurso real de uma rota
    public class LerPontoTrilhaDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime RegistradaEm { get; set; }
    }
}
