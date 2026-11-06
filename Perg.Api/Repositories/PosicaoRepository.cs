using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class PosicaoRepository : IPosicaoRepository
    {
        private readonly PergDbContext _context;

        public PosicaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public Entregador? ObterEntregador(Guid entregadorId)
        {
            return _context.Entregadores.AsNoTracking().FirstOrDefault(e => e.Id == entregadorId);
        }

        public bool TemJornadaAberta(Guid entregadorId)
        {
            return _context.Jornadas.Any(j => j.EntregadorId == entregadorId && j.EncerradaEm == null);
        }

        public Rota? ObterRotaEmAndamento(Guid entregadorId)
        {
            return _context.Rotas
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(r => r.Paradas)
                .FirstOrDefault(r => r.EntregadorId == entregadorId && r.Status == StatusRota.EmAndamento);
        }

        public void Inserir(List<PosicaoEntregador> posicoes)
        {
            using var transacao = _context.Database.BeginTransaction();

            foreach (PosicaoEntregador p in posicoes)
            {
                _context.Database.ExecuteSql($@"
                    INSERT INTO posicoes_entregador
                        (entregador_id, registrada_em, empresa_id, rota_id, latitude, longitude, precisao_m, velocidade_kmh)
                    VALUES
                        ({p.EntregadorId}, {p.RegistradaEm}, {p.EmpresaId}, {p.RotaId}, {p.Latitude}, {p.Longitude}, {p.PrecisaoM}, {p.VelocidadeKmh})
                    ON CONFLICT (entregador_id, registrada_em) DO NOTHING");
            }

            transacao.Commit();
        }

        public List<PosicaoEntregador> ListarTrilha(Guid rotaId)
        {
            // posicoes_entregador tem empresa_id: o filtro por empresa garante que é rota da loja logada
            return _context.PosicoesEntregador
                .AsNoTracking()
                .Where(p => p.RotaId == rotaId)
                .OrderBy(p => p.RegistradaEm)
                .ToList();
        }
    }
}
