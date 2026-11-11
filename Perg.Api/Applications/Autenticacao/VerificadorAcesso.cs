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

        public VerificadorAcesso(ICacheCurto cache, IUsuarioRepository usuarios, IEntregadorRepository entregadores)
        {
            _cache = cache;
            _usuarios = usuarios;
            _entregadores = entregadores;
        }

        public bool UsuarioPodeAcessar(Guid usuarioId)
        {
            return Conferir(ChaveUsuario(usuarioId), () =>
            {
                Usuario? usuario = _usuarios.ObterPorId(usuarioId);
                return usuario != null && usuario.Ativo && usuario.Empresa.Status == StatusEmpresa.Ativa;
            });
        }

        public bool EntregadorPodeAcessar(Guid entregadorId)
        {
            return Conferir(ChaveEntregador(entregadorId), () =>
            {
                Entregador? entregador = _entregadores.ObterPorId(entregadorId);
                return entregador != null && entregador.Status == StatusEntregador.Ativo;
            });
        }

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
    }
}
