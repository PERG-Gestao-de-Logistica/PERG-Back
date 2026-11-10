using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.EquipeDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Equipe da loja: o admin cadastra, edita, desativa/reativa membros e redefine senhas.
    // Novo membro recebe uma senha provisória (devolvida uma vez) e precisa trocá-la no primeiro acesso.
    // Quando houver envio de e-mail, a senha provisória passa a ir por e-mail em vez de voltar na resposta.
    public class EquipeService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public EquipeService(IUsuarioRepository repository, IPasswordHasher<Usuario> passwordHasher)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
        }

        public List<LerMembroDto> Listar(bool incluirInativos)
        {
            return _repository.ListarDaEmpresa(incluirInativos).Select(ParaDto).ToList();
        }

        public MembroComSenhaDto Criar(CriarMembroDto dto, Guid usuarioLogadoId)
        {
            Usuario admin = ExigirAdmin(usuarioLogadoId);

            string nome = (dto.Nome ?? string.Empty).Trim();
            string email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

            ValidarDados(nome, dto.Telefone, dto.Papel);

            if (email.Length == 0 || email.Length > 255 || !email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
            {
                throw new DomainException("E-mail inválido.");
            }

            if (_repository.EmailExiste(email))
            {
                throw new DomainException("Já existe um usuário com esse e-mail.");
            }

            string senhaTemporaria = GeradorSenhaTemporaria.Gerar();

            var membro = new Usuario
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = admin.EmpresaId,
                Nome = nome,
                Email = email,
                Telefone = Limpar(dto.Telefone),
                Papel = dto.Papel,
                Ativo = true,
                TrocarSenha = true,
                NotifOcorrencias = true,
                NotifCobranca = dto.Papel == PapelUsuario.Admin
            };
            membro.SenhaHash = _passwordHasher.HashPassword(membro, senhaTemporaria);

            _repository.Adicionar(membro);

            return new MembroComSenhaDto { Membro = ParaDto(membro), SenhaTemporaria = senhaTemporaria };
        }

        public LerMembroDto Atualizar(Guid id, AtualizarMembroDto dto, Guid usuarioLogadoId)
        {
            ExigirAdmin(usuarioLogadoId);
            Usuario membro = ObterMembro(id);

            string nome = (dto.Nome ?? string.Empty).Trim();
            ValidarDados(nome, dto.Telefone, dto.Papel);

            // Não deixar a empresa sem nenhum admin ativo
            if (membro.Papel == PapelUsuario.Admin && dto.Papel != PapelUsuario.Admin && membro.Ativo
                && _repository.ContarAdminsAtivos() <= 1)
            {
                throw new DomainException("A empresa precisa ter pelo menos um administrador ativo.");
            }

            membro.Nome = nome;
            membro.Telefone = Limpar(dto.Telefone);
            membro.Papel = dto.Papel;
            _repository.Atualizar(membro);

            return ParaDto(membro);
        }

        public LerMembroDto Desativar(Guid id, Guid usuarioLogadoId)
        {
            ExigirAdmin(usuarioLogadoId);

            if (id == usuarioLogadoId)
            {
                throw new DomainException("Você não pode desativar o seu próprio acesso.");
            }

            Usuario membro = ObterMembro(id);
            if (!membro.Ativo)
            {
                return ParaDto(membro);
            }

            if (membro.Papel == PapelUsuario.Admin && _repository.ContarAdminsAtivos() <= 1)
            {
                throw new DomainException("A empresa precisa ter pelo menos um administrador ativo.");
            }

            membro.Ativo = false;
            _repository.Atualizar(membro);
            return ParaDto(membro);
        }

        public LerMembroDto Reativar(Guid id, Guid usuarioLogadoId)
        {
            ExigirAdmin(usuarioLogadoId);
            Usuario membro = ObterMembro(id);

            membro.Ativo = true;
            _repository.Atualizar(membro);
            return ParaDto(membro);
        }

        public MembroComSenhaDto RedefinirSenha(Guid id, Guid usuarioLogadoId)
        {
            ExigirAdmin(usuarioLogadoId);

            if (id == usuarioLogadoId)
            {
                throw new DomainException("Para a sua própria senha, use Usuarios/eu/senha.");
            }

            Usuario membro = ObterMembro(id);
            string senhaTemporaria = GeradorSenhaTemporaria.Gerar();

            membro.SenhaHash = _passwordHasher.HashPassword(membro, senhaTemporaria);
            membro.TrocarSenha = true;
            _repository.Atualizar(membro);

            return new MembroComSenhaDto { Membro = ParaDto(membro), SenhaTemporaria = senhaTemporaria };
        }

        // ---------- apoio ----------

        // O papel é conferido no banco (não só no token): quem perdeu o admin perde a permissão na hora
        private Usuario ExigirAdmin(Guid usuarioLogadoId)
        {
            Usuario? usuario = _repository.ObterPorId(usuarioLogadoId);
            if (usuario == null || !usuario.Ativo || usuario.Papel != PapelUsuario.Admin)
            {
                throw new AcessoNegadoException("Só o administrador da empresa pode gerenciar a equipe.");
            }
            return usuario;
        }

        // O filtro por empresa garante que só aparecem membros da mesma loja
        private Usuario ObterMembro(Guid id)
        {
            return _repository.ObterPorId(id) ?? throw new DomainException("Membro da equipe não encontrado.");
        }

        private static void ValidarDados(string nome, string? telefone, PapelUsuario papel)
        {
            if (nome.Length == 0 || nome.Length > 150)
            {
                throw new DomainException("Informe o nome (até 150 caracteres).");
            }

            if (!Enum.IsDefined(papel))
            {
                throw new DomainException("Papel inválido. Use admin ou funcionario.");
            }

            if (Limpar(telefone)?.Length > 20)
            {
                throw new DomainException("Telefone muito longo.");
            }
        }

        private static string? Limpar(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }

        private static LerMembroDto ParaDto(Usuario u)
        {
            return new LerMembroDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Telefone = u.Telefone,
                Papel = u.Papel,
                Ativo = u.Ativo,
                PrimeiroAcessoPendente = u.TrocarSenha,
                UltimoAcessoEm = u.UltimoAcessoEm,
                CriadoEm = u.CriadoEm
            };
        }
    }
}
