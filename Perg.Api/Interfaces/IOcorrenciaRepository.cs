using Perg.Api.Domains;

namespace Perg.Api.Interfaces
{
    // Usado pela loja (filtro por empresa ativo)
    public interface IOcorrenciaRepository
    {
        List<Ocorrencia> Listar(bool somenteAbertas);
        Ocorrencia? ObterPorId(Guid id);
        void Adicionar(object entidade);
        void Salvar();
    }
}
