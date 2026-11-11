using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Autenticacao
{
    // Confere a cada requisição se quem está com o token ainda pode entrar (usuário ativo, empresa ativa,
    // entregador ativo). O resultado fica 1 minuto em cache; desativar alguém apaga o cache na hora.
    public class VerificadorAcesso
    {
        private static readonly TimeSpan Validade = TimeSpan.FromMinutes(1);

        private readonly ICacheCurto _cache;
        private readonly IUsuarioRepository _usuarios;
        private readonly IEntregadorRepository _entregadores;
        private readonly IBackofficeRepository _backoffice;

        public VerificadorAcesso(ICacheCurto cache, IUsuarioRepository usuarios, IEntregadorRepository entregadores, IBackofficeRepository backoffice)
        {
            _cache = cache;
            _usuarios = usuarios;
            _entregadores = entregadores;
            _backoffice = backoffice;
        }

        public bool UsuarioPodeAcessar(Guid usuarioId)
        {
            return Conferir(ChaveUsuario(usuarioId), () =>
            {
                Usuario? usuario = _usuarios.ObterPorId(usuarioId);
                return usuario != null && usuario.Ativo && usuario.Empresa.Status == StatusEmpresa.Ativa;
            });
        }

        public bool EntregadorPodeAcessar(Guid entregadorId) => StatusDoEntregador(entregadorId) == StatusEntregador.Ativo;

        // Status atual do entregador (null = não existe), com o mesmo cache de 1 minuto
        public StatusEntregador? StatusDoEntregador(Guid entregadorId)
        {
            string chave = ChaveEntregador(entregadorId);
            string? guardado = _cache.Obter(chave);
            if (guardado != null)
            {
                return Enum.TryParse(guardado, out StatusEntregador status) ? status : null;
            }

            StatusEntregador? atual = _entregadores.ObterPorId(entregadorId)?.Status;
            _cache.Gravar(chave, atual?.ToString() ?? "nenhum", Validade);
            return atual;
        }

        public bool AdministradorPodeAcessar(Guid administradorId)
        {
            return Conferir(ChaveAdministrador(administradorId), () => _backoffice.ObterAdministrador(administradorId)?.Ativo == true);
        }

        public void EsquecerAdministrador(Guid administradorId) => _cache.Remover(ChaveAdministrador(administradorId));

        public void EsquecerUsuario(Guid usuarioId) => _cache.Remover(ChaveUsuario(usuarioId));

        public void EsquecerEntregador(Guid entregadorId) => _cache.Remover(ChaveEntregador(entregadorId));

        private bool Conferir(string chave, Func<bool> consultarBanco)
        {
            string? guardado = _cache.Obter(chave);
            if (guardado != null)
            {
                return guardado == "1";
            }

            bool pode = consultarBanco();
            _cache.Gravar(chave, pode ? "1" : "0", Validade);
            return pode;
        }

        private static string ChaveUsuario(Guid id) => $"acesso:usuario:{id}";
        private static string ChaveEntregador(Guid id) => $"acesso:entregador:{id}";
        private static string ChaveAdministrador(Guid id) => $"acesso:administrador:{id}";
    }
}
