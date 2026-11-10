using Microsoft.EntityFrameworkCore;
using Perg.Api.Applications.Importacoes;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Repositories
{
    public class ImportacaoRepository : IImportacaoRepository
    {
        private readonly PergDbContext _context;

        public ImportacaoRepository(PergDbContext context)
        {
            _context = context;
        }

        public void Adicionar(Importacao importacao)
        {
            _context.Importacoes.Add(importacao);
            _context.SaveChanges();
        }

        public Importacao? ObterPorId(Guid id)
        {
            return _context.Importacoes.FirstOrDefault(i => i.Id == id);
        }

        public List<Importacao> Listar(int quantidade)
        {
            return _context.Importacoes
                .AsNoTracking()
                .OrderByDescending(i => i.CriadoEm)
                .Take(quantidade)
                .ToList();
        }

        public Importacao? ObterProximaParaProcessar()
        {
            return _context.Importacoes
                .IgnoreQueryFilters()
                .Where(i => i.Status == StatusImportacao.Processando)
                .OrderBy(i => i.CriadoEm)
                .FirstOrDefault();
        }

        public void AdicionarNotificacao(Notificacao notificacao)
        {
            _context.Notificacoes.Add(notificacao);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }
    }
}
