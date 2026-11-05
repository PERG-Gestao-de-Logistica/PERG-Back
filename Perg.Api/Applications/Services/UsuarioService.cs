using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.UsuariosDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class UsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly GeradorTokenJwt _geradorToken;

        public UsuarioService(IUsuarioRepository repository, IPasswordHasher<Usuario> passwordHasher, GeradorTokenJwt geradorToken)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _geradorToken = geradorToken;
        }

        public string Login(LoginUsuarioDto dto)
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

            return _geradorToken.GerarToken(usuario);
        }

        public LerUsuarioDto ObterPerfil(Guid id)
        {
            Usuario? usuario = _repository.ObterPorId(id);

            if (usuario == null)
            {
                throw new DomainException("Usuário não encontrado.");
            }

            return new LerUsuarioDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Papel = usuario.Papel,
                TrocarSenha = usuario.TrocarSenha,
                EmpresaId = usuario.EmpresaId,
                NomeEmpresa = usuario.Empresa.NomeFantasia
            };
        }
    }
}
