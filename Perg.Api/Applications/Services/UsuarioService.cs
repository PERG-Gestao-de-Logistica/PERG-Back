using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.UsuariosDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public record ResultadoLogin(string Token, bool TrocarSenha);

    public class UsuarioService
    {
        private const int TamanhoMinimoSenha = 8;

        private readonly IUsuarioRepository _repository;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly GeradorTokenJwt _geradorToken;

        public UsuarioService(IUsuarioRepository repository, IPasswordHasher<Usuario> passwordHasher, GeradorTokenJwt geradorToken)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _geradorToken = geradorToken;
        }

        public ResultadoLogin Login(LoginUsuarioDto dto)
        {
            string email = dto.Email.Trim().ToLowerInvariant();
            Usuario? usuario = _repository.ObterPorEmail(email);

            // Mesma mensagem para e-mail inexistente e senha errada (não revela quem tem conta)
            if (usuario == null || usuario.SenhaHash == null)
            {
                throw new DomainException("E-mail ou senha inválidos.");
            }

            PasswordVerificationResult resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);

            if (resultado == PasswordVerificationResult.Failed)
            {
                throw new DomainException("E-mail ou senha inválidos.");
            }

            if (!usuario.Ativo || usuario.Empresa.Status != StatusEmpresa.Ativa)
            {
                throw new DomainException("Acesso desativado. Fale com o administrador da sua empresa.");
            }

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);
            }

            usuario.UltimoAcessoEm = DateTime.UtcNow;
            _repository.Atualizar(usuario);

            // trocarSenha = true: o front leva direto para a tela de nova senha (primeiro acesso ou senha redefinida)
            return new ResultadoLogin(_geradorToken.GerarToken(usuario), usuario.TrocarSenha);
        }

        public LerUsuarioDto ObterPerfil(Guid id)
        {
            return ParaDto(ObterUsuario(id));
        }

        public LerUsuarioDto AtualizarPerfil(Guid id, AtualizarPerfilDto dto)
        {
            Usuario usuario = ObterUsuario(id);

            string nome = (dto.Nome ?? string.Empty).Trim();
            if (nome.Length == 0 || nome.Length > 150)
            {
                throw new DomainException("Informe o nome (até 150 caracteres).");
            }

            string? telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();
            if (telefone?.Length > 20)
            {
                throw new DomainException("Telefone muito longo.");
            }

            usuario.Nome = nome;
            usuario.Telefone = telefone;
            usuario.NotifOcorrencias = dto.NotifOcorrencias;
            usuario.NotifCobranca = dto.NotifCobranca;
            _repository.Atualizar(usuario);

            return ParaDto(usuario);
        }

        public void TrocarSenha(Guid id, TrocarSenhaDto dto)
        {
            Usuario usuario = ObterUsuario(id);

            if (usuario.SenhaHash == null
                || _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.SenhaAtual) == PasswordVerificationResult.Failed)
            {
                throw new DomainException("Senha atual incorreta.");
            }

            if ((dto.NovaSenha ?? string.Empty).Length < TamanhoMinimoSenha)
            {
                throw new DomainException($"A nova senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
            }

            if (dto.NovaSenha == dto.SenhaAtual)
            {
                throw new DomainException("A nova senha precisa ser diferente da atual.");
            }

            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.NovaSenha!);
            usuario.TrocarSenha = false;
            _repository.Atualizar(usuario);
        }

        // ---------- apoio ----------

        private Usuario ObterUsuario(Guid id)
        {
            return _repository.ObterPorId(id) ?? throw new DomainException("Usuário não encontrado.");
        }

        private static LerUsuarioDto ParaDto(Usuario usuario)
        {
            return new LerUsuarioDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                Papel = usuario.Papel,
                TrocarSenha = usuario.TrocarSenha,
                NotifOcorrencias = usuario.NotifOcorrencias,
                NotifCobranca = usuario.NotifCobranca,
                EmpresaId = usuario.EmpresaId,
                NomeEmpresa = usuario.Empresa.NomeFantasia
            };
        }
    }
}
