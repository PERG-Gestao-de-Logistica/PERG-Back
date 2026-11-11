using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Interfaces
{
    public interface IBackofficeRepository
    {
        // Administradores
        Administrador? ObterAdministrador(Guid id);
        Administrador? ObterAdministradorPorEmail(string email);
        List<Administrador> ListarAdministradores();
        bool ExisteAdministrador();
        bool EmailAdministradorExiste(string email);
        void AdicionarAdministrador(Administrador administrador);

        // Entregadores (com veículo atual e documentos)
        List<Entregador> ListarEntregadores(StatusEntregador? status, string? busca, int quantidade);
        Entregador? ObterEntregador(Guid id);
        DocumentoEntregador? ObterDocumento(Guid id);
        bool TemRotaEmAndamento(Guid entregadorId);

        void Salvar();
    }
}
