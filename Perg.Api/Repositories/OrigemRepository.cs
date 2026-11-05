using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    // O filtro por empresa do contexto já garante que só aparecem as origens da loja logada
    public class OrigemRepository : IOrigemRepository
    {
        private readonly PergDbContext _context;

        public OrigemRepository(PergDbContext context)
        {
            _context = context;
        }

        public List<Origem> Listar()
        {
            return _context.Origens
                .Where(o => o.Ativa)
                .OrderByDescending(o => o.Padrao)
                .ThenBy(o => o.Nome)
                .ToList();
        }

        public Origem? ObterPorId(Guid id)
        {
            return _context.Origens.FirstOrDefault(o => o.Id == id);
        }

        public Origem? ObterPadrao()
        {
            return _context.Origens.FirstOrDefault(o => o.Padrao && o.Ativa);
        }

        public void Adicionar(Origem origem)
        {
            // Só pode existir uma origem padrão por empresa (índice único no banco).
            // Primeiro desmarca a atual, depois grava a nova, tudo na mesma transação.
            using var transacao = _context.Database.BeginTransaction();

            if (origem.Padrao)
            {
                Origem? padraoAtual = _context.Origens.FirstOrDefault(o => o.Padrao);
                if (padraoAtual != null)
                {
                    padraoAtual.Padrao = false;
                    _context.SaveChanges();
                }
            }

            _context.Origens.Add(origem);
            _context.SaveChanges();

            transacao.Commit();
        }
    }
}
