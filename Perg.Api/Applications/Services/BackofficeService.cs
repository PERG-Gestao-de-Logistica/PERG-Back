using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Email;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.BackofficeDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Painel interno do Perg: login da equipe, análise de documentos e aprovação de entregadores.
    public class BackofficeService
    {
        private const int TamanhoMinimoSenha = 8;

        private readonly IBackofficeRepository _repository;
        private readonly IPasswordHasher<Administrador> _hasher;
        private readonly GeradorTokenJwt _geradorToken;
        private readonly VerificadorAcesso _acesso;
        private readonly IEnvioEmail _email;
        private readonly IArmazenamentoArquivos _armazenamento;

        public BackofficeService(IBackofficeRepository repository, IPasswordHasher<Administrador> hasher, GeradorTokenJwt geradorToken,
            VerificadorAcesso acesso, IEnvioEmail email, IArmazenamentoArquivos armazenamento)
        {
            _repository = repository;
            _hasher = hasher;
            _geradorToken = geradorToken;
            _acesso = acesso;
            _email = email;
            _armazenamento = armazenamento;
        }

        // ---------- Acesso ----------

        public (string Token, bool TrocarSenha) Login(LoginAdministradorDto dto)
        {
            Administrador? admin = _repository.ObterAdministradorPorEmail(Normalizar(dto.Email));

            if (admin == null || _hasher.VerifyHashedPassword(admin, admin.SenhaHash, dto.Senha) == PasswordVerificationResult.Failed)
            {
                throw new DomainException("E-mail ou senha inválidos.");
            }

            if (!admin.Ativo)
            {
                throw new DomainException("Acesso desativado.");
            }

            admin.UltimoAcessoEm = DateTime.UtcNow;
            _repository.Salvar();
            return (_geradorToken.GerarTokenAdministrador(admin), admin.TrocarSenha);
        }

        public void TrocarSenha(Guid administradorId, TrocarSenhaAdministradorDto dto)
        {
            Administrador admin = _repository.ObterAdministrador(administradorId) ?? throw new DomainException("Administrador não encontrado.");

            if (_hasher.VerifyHashedPassword(admin, admin.SenhaHash, dto.SenhaAtual) == PasswordVerificationResult.Failed)
            {
                throw new DomainException("Senha atual incorreta.");
            }

            ValidarSenha(dto.NovaSenha);
            admin.SenhaHash = _hasher.HashPassword(admin, dto.NovaSenha);
            admin.TrocarSenha = false;
            _repository.Salvar();
        }

        // Primeiro acesso: cria o administrador inicial dos Segredos do Usuário (Backoffice:EmailInicial / SenhaInicial)
        public void GarantirAdministradorInicial(string? email, string? senha)
        {
            if (_repository.ExisteAdministrador() || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                return;
            }

            var admin = new Administrador
            {
                Id = Guid.CreateVersion7(),
                Nome = "Administrador Perg",
                Email = Normalizar(email),
                Ativo = true
            };
            admin.SenhaHash = _hasher.HashPassword(admin, senha);
            _repository.AdicionarAdministrador(admin);
        }

        // ---------- Administradores ----------

        public List<LerAdministradorDto> ListarAdministradores() => _repository.ListarAdministradores().Select(ParaDto).ToList();

        public (LerAdministradorDto Administrador, bool EmailEnviado, string? SenhaTemporaria) CriarAdministrador(CriarAdministradorDto dto)
        {
            string nome = (dto.Nome ?? string.Empty).Trim();
            string email = Normalizar(dto.Email);

            if (nome.Length == 0 || nome.Length > 150) throw new DomainException("Informe o nome (até 150 caracteres).");
            if (!email.Contains('@') || email.Length > 255) throw new DomainException("E-mail inválido.");
            if (_repository.EmailAdministradorExiste(email)) throw new DomainException("Já existe um administrador com esse e-mail.");

            string senhaTemporaria = GeradorSenhaTemporaria.Gerar();
            var admin = new Administrador
            {
                Id = Guid.CreateVersion7(),
                Nome = nome,
                Email = email,
                Ativo = true,
                TrocarSenha = true
            };
            admin.SenhaHash = _hasher.HashPassword(admin, senhaTemporaria);
            _repository.AdicionarAdministrador(admin);

            (string assunto, string html) = ModelosEmail.NovoAdministrador(nome, email, senhaTemporaria);
            bool enviado = _email.Enviar(email, assunto, html);
            return (ParaDto(admin), enviado, enviado ? null : senhaTemporaria);
        }

        public LerAdministradorDto DefinirAtivoAdministrador(Guid id, bool ativo, Guid administradorLogadoId)
        {
            if (id == administradorLogadoId && !ativo)
            {
                throw new DomainException("Você não pode desativar o seu próprio acesso.");
            }

            Administrador admin = _repository.ObterAdministrador(id) ?? throw new DomainException("Administrador não encontrado.");
            admin.Ativo = ativo;
            _repository.Salvar();
            _acesso.EsquecerAdministrador(id);
            return ParaDto(admin);
        }

        // ---------- Entregadores ----------

        public List<LerEntregadorResumoBackofficeDto> ListarEntregadores(StatusEntregador? status, string? busca)
        {
            return _repository.ListarEntregadores(status, busca, 200).Select(e => PreencherResumo(new LerEntregadorResumoBackofficeDto(), e)).ToList();
        }

        public LerEntregadorBackofficeDto ObterEntregador(Guid id)
        {
            Entregador entregador = _repository.ObterEntregador(id) ?? throw new DomainException("Entregador não encontrado.");
            return ParaDto(entregador);
        }

        public LerEntregadorBackofficeDto AprovarEntregador(Guid id, Guid administradorId)
        {
            Entregador entregador = _repository.ObterEntregador(id) ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.Status != StatusEntregador.EmAnalise)
            {
                throw new DomainException("Só cadastros em análise podem ser aprovados.");
            }

            List<string> pendencias = Pendencias(entregador);
            if (pendencias.Count > 0)
            {
                throw new DomainException("Ainda não dá para aprovar: " + string.Join("; ", pendencias) + ".");
            }

            // O banco exige celular confirmado para ficar ativo (ck_entregadores_ativo).
            // Enquanto não há código por SMS, a confirmação é feita pela equipe na análise do cadastro.
            entregador.TelefoneConfirmado = true;
            entregador.Status = StatusEntregador.Ativo;
            entregador.AprovadoEm = DateTime.UtcNow;
            entregador.AprovadoPor = administradorId;
            if (entregador.VeiculoAtual != null) entregador.VeiculoAtual.Verificado = true;
            _repository.Salvar();
            _acesso.EsquecerEntregador(id);

            (string assunto, string html) = ModelosEmail.CadastroEntregadorAprovado(entregador.Nome);
            _email.Enviar(entregador.Email, assunto, html);
            return ParaDto(entregador);
        }

        // Cadastro recusado: fica inativo e o entregador recebe o motivo por e-mail
        public LerEntregadorBackofficeDto ReprovarEntregador(Guid id, MotivoDto dto)
        {
            Entregador entregador = _repository.ObterEntregador(id) ?? throw new DomainException("Entregador não encontrado.");
            string motivo = ExigirMotivo(dto);

            if (entregador.Status != StatusEntregador.EmAnalise)
            {
                throw new DomainException("Só cadastros em análise podem ser reprovados. Para quem já está ativo, use suspender.");
            }

            entregador.Status = StatusEntregador.Inativo;
            _repository.Salvar();
            _acesso.EsquecerEntregador(id);

            (string assunto, string html) = ModelosEmail.CadastroEntregadorReprovado(entregador.Nome, motivo);
            _email.Enviar(entregador.Email, assunto, html);
            return ParaDto(entregador);
        }

        public LerEntregadorBackofficeDto SuspenderEntregador(Guid id, MotivoDto dto)
        {
            Entregador entregador = _repository.ObterEntregador(id) ?? throw new DomainException("Entregador não encontrado.");
            string motivo = ExigirMotivo(dto);

            if (entregador.Status != StatusEntregador.Ativo)
            {
                throw new DomainException("Só entregadores ativos podem ser suspensos.");
            }

            if (_repository.TemRotaEmAndamento(id))
            {
                throw new DomainException("O entregador está com uma rota em andamento. Suspenda depois que ela terminar.");
            }

            entregador.Status = StatusEntregador.Suspenso;
            _repository.Salvar();
            _acesso.EsquecerEntregador(id); // o app dele para de funcionar já na próxima requisição

            (string assunto, string html) = ModelosEmail.EntregadorSuspenso(entregador.Nome, motivo);
            _email.Enviar(entregador.Email, assunto, html);
            return ParaDto(entregador);
        }

        public LerEntregadorBackofficeDto ReativarEntregador(Guid id)
        {
            Entregador entregador = _repository.ObterEntregador(id) ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.Status != StatusEntregador.Suspenso)
            {
                throw new DomainException("Só entregadores suspensos podem ser reativados.");
            }

            entregador.Status = StatusEntregador.Ativo;
            _repository.Salvar();
            _acesso.EsquecerEntregador(id);
            return ParaDto(entregador);
        }

        // ---------- Documentos ----------

        public (Stream Conteudo, string ContentType) AbrirArquivoDocumento(Guid documentoId)
        {
            DocumentoEntregador documento = _repository.ObterDocumento(documentoId) ?? throw new DomainException("Documento não encontrado.");
            if (documento.ArquivoChave == null) throw new DomainException("Este documento não tem arquivo.");

            Stream conteudo = _armazenamento.Abrir(documento.ArquivoChave) ?? throw new DomainException("Arquivo não encontrado.");
            string tipo = Path.GetExtension(documento.ArquivoChave).ToLowerInvariant() switch
            {
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            return (conteudo, tipo);
        }

        public LerDocumentoBackofficeDto AprovarDocumento(Guid documentoId, Guid administradorId)
        {
            DocumentoEntregador documento = _repository.ObterDocumento(documentoId) ?? throw new DomainException("Documento não encontrado.");

            if (documento.Validade.HasValue && documento.Validade.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new DomainException("Documento vencido. Rejeite e peça um novo.");
            }

            documento.Status = StatusDocumento.Aprovado;
            documento.MotivoRejeicao = null;
            documento.AnalisadoEm = DateTime.UtcNow;
            documento.AnalisadoPor = administradorId;
            _repository.Salvar();
            return ParaDto(documento);
        }

        public LerDocumentoBackofficeDto RejeitarDocumento(Guid documentoId, MotivoDto dto, Guid administradorId)
        {
            DocumentoEntregador documento = _repository.ObterDocumento(documentoId) ?? throw new DomainException("Documento não encontrado.");
            string motivo = ExigirMotivo(dto);

            documento.Status = StatusDocumento.Rejeitado;
            documento.MotivoRejeicao = motivo;
            documento.AnalisadoEm = DateTime.UtcNow;
            documento.AnalisadoPor = administradorId;
            _repository.Salvar();

            (string assunto, string html) = ModelosEmail.DocumentoRejeitado(documento.Entregador.Nome, NomeDocumento(documento.Tipo), motivo);
            _email.Enviar(documento.Entregador.Email, assunto, html);
            return ParaDto(documento);
        }

        // ---------- apoio ----------

        // Regras para aprovar: selfie e documento com foto sempre; CNH para moto/carro/van/caminhão; CRLV quando há placa.
        // Nada pendente ou rejeitado, nada vencido.
        private static List<string> Pendencias(Entregador e)
        {
            var pendencias = new List<string>();
            List<DocumentoEntregador> docs = e.DocumentosEntregador.ToList();
            DateOnly hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            bool Aprovado(TipoDocumento tipo) => docs.Any(d => d.Tipo == tipo && d.Status == StatusDocumento.Aprovado
                && (d.Validade == null || d.Validade >= hoje));

            if (!Aprovado(TipoDocumento.Selfie)) pendencias.Add("selfie aprovada");

            bool motorizado = e.VeiculoAtual != null && e.VeiculoAtual.Tipo != TipoVeiculo.Bike;
            if (motorizado && !Aprovado(TipoDocumento.Cnh)) pendencias.Add("CNH aprovada e dentro da validade");
            if (!motorizado && !Aprovado(TipoDocumento.Cnh) && !Aprovado(TipoDocumento.DocumentoIdentidade))
            {
                pendencias.Add("documento de identidade aprovado");
            }

            if (!string.IsNullOrWhiteSpace(e.VeiculoAtual?.Placa) && !Aprovado(TipoDocumento.Crlv)) pendencias.Add("CRLV do veículo aprovado");

            int emAberto = docs.Count(d => d.Status == StatusDocumento.Pendente);
            if (emAberto > 0) pendencias.Add($"{emAberto} documento(s) aguardando análise");

            return pendencias;
        }

        private static string NomeDocumento(TipoDocumento tipo) => tipo switch
        {
            TipoDocumento.Cnh => "CNH",
            TipoDocumento.Crlv => "CRLV",
            TipoDocumento.Selfie => "selfie",
            _ => "documento de identidade"
        };

        private static string ExigirMotivo(MotivoDto dto)
        {
            string motivo = (dto.Motivo ?? string.Empty).Trim();
            if (motivo.Length < 5) throw new DomainException("Informe o motivo (será enviado ao entregador).");
            return motivo.Length > 500 ? motivo[..500] : motivo;
        }

        private static void ValidarSenha(string? senha)
        {
            if ((senha ?? string.Empty).Length < TamanhoMinimoSenha)
            {
                throw new DomainException($"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
            }
        }

        private static string Normalizar(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

        private static T PreencherResumo<T>(T dto, Entregador e) where T : LerEntregadorResumoBackofficeDto
        {
            dto.Id = e.Id;
            dto.Nome = e.Nome;
            dto.Cpf = e.Cpf;
            dto.Telefone = e.Telefone;
            dto.Status = e.Status;
            dto.TipoVeiculo = e.VeiculoAtual?.Tipo;
            dto.DocumentosPendentes = e.DocumentosEntregador.Count(d => d.Status == StatusDocumento.Pendente);
            dto.CriadoEm = e.CriadoEm;
            return dto;
        }

        private static LerEntregadorBackofficeDto ParaDto(Entregador e)
        {
            var dto = PreencherResumo(new LerEntregadorBackofficeDto(), e);
            dto.Email = e.Email;
            dto.Endereco = $"{e.Logradouro}, {e.Numero}{(string.IsNullOrWhiteSpace(e.Complemento) ? "" : " " + e.Complemento)} - {e.Bairro}, {e.Cidade}/{e.Uf}";
            dto.Placa = e.VeiculoAtual?.Placa;
            dto.ModeloVeiculo = e.VeiculoAtual?.Modelo;
            dto.AprovadoEm = e.AprovadoEm;
            dto.Documentos = e.DocumentosEntregador.OrderBy(d => d.Tipo).ThenByDescending(d => d.CriadoEm).Select(ParaDto).ToList();
            dto.Pendencias = e.Status == StatusEntregador.EmAnalise ? Pendencias(e) : new List<string>();
            return dto;
        }

        private static LerDocumentoBackofficeDto ParaDto(DocumentoEntregador d) => new()
        {
            Id = d.Id,
            Tipo = d.Tipo,
            Status = d.Status,
            Numero = d.Numero,
            Categoria = d.Categoria,
            Ear = d.Ear,
            Validade = d.Validade,
            MotivoRejeicao = d.MotivoRejeicao,
            AnalisadoEm = d.AnalisadoEm,
            UrlArquivo = d.ArquivoChave != null ? $"/api/backoffice/documentos/{d.Id}/arquivo" : null,
            CriadoEm = d.CriadoEm
        };

        private static LerAdministradorDto ParaDto(Administrador a) => new()
        {
            Id = a.Id,
            Nome = a.Nome,
            Email = a.Email,
            Ativo = a.Ativo,
            TrocarSenha = a.TrocarSenha,
            UltimoAcessoEm = a.UltimoAcessoEm
        };
    }
}
