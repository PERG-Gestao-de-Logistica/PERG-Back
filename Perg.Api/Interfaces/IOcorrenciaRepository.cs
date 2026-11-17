using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    public interface IOcorrenciaRepository
    {
        List<Ocorrencia> Listar(bool somenteAbertas);
        Ocorrencia? ObterPorId(Guid id);
        void Adicionar(object entidade);
        void Salvar();
    }
}
