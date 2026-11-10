using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IImportacaoRepository
    {
        void Adicionar(Importacao importacao);
        Importacao? ObterPorId(Guid id);
        List<Importacao> Listar(int quantidade);

        // Processador em segundo plano (sem filtro de empresa)
        Importacao? ObterProximaParaProcessar();

        void AdicionarNotificacao(Notificacao notificacao);
        void Salvar();
    }
}
