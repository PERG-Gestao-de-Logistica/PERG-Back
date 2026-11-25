using Microsoft.EntityFrameworkCore;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
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
                DesmarcarPadraoAtual(origem.Id);
            }

            _context.Origens.Add(origem);
            _context.SaveChanges();

            transacao.Commit();
        }

        public void Atualizar(Origem origem)
        {
            using var transacao = _context.Database.BeginTransaction();

            if (origem.Padrao)
            {
                DesmarcarPadraoAtual(origem.Id);
            }

            _context.Origens.Update(origem);
            _context.SaveChanges();

            transacao.Commit();
        }

        // UPDATE direto no banco: não grava junto as alterações da origem que está sendo salva,
        // senão as duas ficariam padrão ao mesmo tempo e o índice único reclamaria
        private void DesmarcarPadraoAtual(Guid exceto)
        {
            _context.Origens
                .Where(o => o.Padrao && o.Id != exceto)
                .ExecuteUpdate(s => s.SetProperty(o => o.Padrao, false));
        }
    }
}
