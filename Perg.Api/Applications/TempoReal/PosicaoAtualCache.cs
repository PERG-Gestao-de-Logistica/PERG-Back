using System.Collections.Concurrent;

namespace Perg.Api.Applications.TempoReal
{
    public class PosicaoAtual
    {
        public Guid EntregadorId { get; set; }
        public string EntregadorNome { get; set; } = string.Empty;
        public Guid? EmpresaId { get; set; }
        public Guid? RotaId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? VelocidadeKmh { get; set; }
        public DateTime RegistradaEm { get; set; }
    }

    // Última posição de cada entregador, em memória (singleton).
    // Com mais de um servidor, isto vai para o Redis, como previsto no modelo.
    public class PosicaoAtualCache
    {
        private static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);
        private readonly ConcurrentDictionary<Guid, PosicaoAtual> _posicoes = new();

        public void Atualizar(PosicaoAtual posicao)
        {
            _posicoes.AddOrUpdate(posicao.EntregadorId, posicao,
                (_, atual) => posicao.RegistradaEm >= atual.RegistradaEm ? posicao : atual);
        }

        // Posição de 10 minutos atrás já não serve para o mapa
        public PosicaoAtual? Obter(Guid entregadorId)
        {
            return _posicoes.TryGetValue(entregadorId, out PosicaoAtual? posicao)
                && DateTime.UtcNow - posicao.RegistradaEm <= Validade
                ? posicao
                : null;
        }

        public List<PosicaoAtual> ListarDaEmpresa(Guid empresaId)
        {
            DateTime limite = DateTime.UtcNow - Validade;
            return _posicoes.Values
                .Where(p => p.EmpresaId == empresaId && p.RegistradaEm >= limite)
                .ToList();
        }
    }
}
