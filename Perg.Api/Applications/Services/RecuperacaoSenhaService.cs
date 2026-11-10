using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Email;
using Perg.Api.Domains;
using Perg.Api.DTOs.RecuperacaoSenhaDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Esqueci a senha (loja e app): código de 6 dígitos por e-mail, válido por 15 min, 5 tentativas.
    // O pedido sempre responde igual, exista ou não a conta (não revela quem está cadastrado).
    public class RecuperacaoSenhaService
    {
        private const int TamanhoMinimoSenha = 8;

        private readonly IUsuarioRepository _usuarios;
        private readonly IEntregadorRepository _entregadores;
        private readonly IPasswordHasher<Usuario> _hasherUsuario;
        private readonly IPasswordHasher<Entregador> _hasherEntregador;
        private readonly ICodigosVerificacao _codigos;
        private readonly IEnvioEmail _email;
        private readonly ILogger<RecuperacaoSenhaService> _logger;

        public RecuperacaoSenhaService(IUsuarioRepository usuarios, IEntregadorRepository entregadores,
            IPasswordHasher<Usuario> hasherUsuario, IPasswordHasher<Entregador> hasherEntregador,
            ICodigosVerificacao codigos, IEnvioEmail email, ILogger<RecuperacaoSenhaService> logger)
        {
            _usuarios = usuarios;
            _entregadores = entregadores;
            _hasherUsuario = hasherUsuario;
            _hasherEntregador = hasherEntregador;
            _codigos = codigos;
            _email = email;
            _logger = logger;
        }

        // ---------- Loja ----------

        public void SolicitarUsuario(EsqueciSenhaUsuarioDto dto)
        {
            Usuario? usuario = _usuarios.ObterPorEmail(NormalizarEmail(dto.Email));
            if (usuario == null || !usuario.Ativo)
            {
                return;
            }

            EnviarCodigo(ChaveUsuario(usuario.Id), usuario.Nome, usuario.Email);
        }

        public void RedefinirUsuario(RedefinirSenhaUsuarioDto dto)
        {
            ValidarNovaSenha(dto.NovaSenha);

            Usuario? usuario = _usuarios.ObterPorEmail(NormalizarEmail(dto.Email));
            if (usuario == null || !usuario.Ativo)
            {
                throw new DomainException("Código inválido ou expirado.");
            }

            ConferirCodigo(ChaveUsuario(usuario.Id), dto.Codigo);

            usuario.SenhaHash = _hasherUsuario.HashPassword(usuario, dto.NovaSenha);
            usuario.TrocarSenha = false;
            usuario.EmailConfirmado = true; // recebeu o código, então o e-mail é dele
            _usuarios.Atualizar(usuario);
        }

        // ---------- App do entregador ----------

        public void SolicitarEntregador(EsqueciSenhaEntregadorDto dto)
        {
            Entregador? entregador = _entregadores.ObterPorLogin(SomenteDigitos(dto.Login));
            if (entregador == null)
            {
                return;
            }

            EnviarCodigo(ChaveEntregador(entregador.Id), entregador.Nome, entregador.Email);
        }

        public void RedefinirEntregador(RedefinirSenhaEntregadorDto dto)
        {
            ValidarNovaSenha(dto.NovaSenha);

            Entregador? entregador = _entregadores.ObterPorLogin(SomenteDigitos(dto.Login));
            if (entregador == null)
            {
                throw new DomainException("Código inválido ou expirado.");
            }

            ConferirCodigo(ChaveEntregador(entregador.Id), dto.Codigo);

            entregador.SenhaHash = _hasherEntregador.HashPassword(entregador, dto.NovaSenha);
            entregador.SenhaAlteradaEm = DateTime.UtcNow;
            entregador.EmailConfirmado = true;
            _entregadores.Salvar();
        }

        // ---------- apoio ----------

        private void EnviarCodigo(string chave, string nome, string email)
        {
            string? codigo = _codigos.Gerar(chave);
            if (codigo == null)
            {
                return; // já mandou um há menos de 1 minuto
            }

            (string assunto, string html) = ModelosEmail.CodigoRedefinicao(nome, codigo);
            if (!_email.Enviar(email, assunto, html))
            {
                _logger.LogWarning("Código de redefinição não enviado por e-mail ({Chave}).", chave);
            }
        }

        private void ConferirCodigo(string chave, string codigo)
        {
            switch (_codigos.Validar(chave, codigo))
            {
                case ResultadoCodigo.Valido:
                    return;
                case ResultadoCodigo.Invalido:
                    throw new DomainException("Código incorreto.");
                case ResultadoCodigo.TentativasEsgotadas:
                    throw new DomainException("Muitas tentativas erradas. Peça um novo código.");
                default:
                    throw new DomainException("Código inválido ou expirado. Peça um novo código.");
            }
        }

        private static void ValidarNovaSenha(string? senha)
        {
            if ((senha ?? string.Empty).Length < TamanhoMinimoSenha)
            {
                throw new DomainException($"A nova senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
            }
        }

        private static string ChaveUsuario(Guid id) => "senha:usuario:" + id;
        private static string ChaveEntregador(Guid id) => "senha:entregador:" + id;
        private static string NormalizarEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
        private static string SomenteDigitos(string? texto) => new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
