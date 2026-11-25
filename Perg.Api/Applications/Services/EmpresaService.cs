using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.EmpresasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class EmpresaService
    {
        private const string VersaoTermos = "2026-11";

        private readonly IEmpresaRepository _repository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IGeocodificador _geocodificador;

        public EmpresaService(IEmpresaRepository repository, IUsuarioRepository usuarioRepository,
            IPasswordHasher<Usuario> passwordHasher, IGeocodificador geocodificador)
        {
            _repository = repository;
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
            _geocodificador = geocodificador;
        }

        public LerEmpresaDto Cadastrar(CadastrarEmpresaDto dto)
        {
            string cnpj = SomenteLetrasENumeros(dto.Cnpj).ToUpperInvariant();
            string emailAdmin = dto.Responsavel.Email.Trim().ToLowerInvariant();

            Validar(dto, cnpj, emailAdmin);

            DateTime agora = DateTime.UtcNow;

            Empresa empresa = new Empresa
            {
                Id = Guid.CreateVersion7(),
                Cnpj = cnpj,
                RazaoSocial = dto.RazaoSocial.Trim(),
                NomeFantasia = dto.NomeFantasia.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Telefone = dto.Telefone,
                Segmento = dto.Segmento,
                FaixaPedidosDia = dto.FaixaPedidosDia,
                EntregaMesmoDia = dto.EntregaMesmoDia,
                EntregaAgendada = dto.EntregaAgendada,
                RetiradaLoja = dto.RetiradaLoja,
                VendeSiteProprio = dto.VendeSiteProprio,
                VendeWhatsapp = dto.VendeWhatsapp,
                VendeShopify = dto.VendeShopify,
                UsaCarro = dto.UsaCarro,
                UsaMoto = dto.UsaMoto,
                UsaVanCaminhao = dto.UsaVanCaminhao,
                Status = StatusEmpresa.Ativa
            };

            Usuario admin = new Usuario
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresa.Id,
                Nome = dto.Responsavel.Nome.Trim(),
                Email = emailAdmin,
                Telefone = dto.Responsavel.Telefone,
                Papel = PapelUsuario.Admin,
                Ativo = true,
                NotifOcorrencias = true,
                NotifCobranca = true,
                TermosVersao = VersaoTermos,
                TermosAceitosEm = agora
            };
            admin.SenhaHash = _passwordHasher.HashPassword(admin, dto.Responsavel.Senha);

            Origem origem = new Origem
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresa.Id,
                ResponsavelId = admin.Id,
                Nome = empresa.NomeFantasia,
                Cep = SomenteLetrasENumeros(dto.Endereco.Cep),
                Logradouro = dto.Endereco.Logradouro.Trim(),
                Numero = dto.Endereco.Numero.Trim(),
                Complemento = dto.Endereco.Complemento,
                Bairro = dto.Endereco.Bairro.Trim(),
                Cidade = dto.Endereco.Cidade.Trim(),
                Uf = dto.Endereco.Uf.Trim().ToUpperInvariant(),
                Abre = dto.Endereco.Abre,
                Fecha = dto.Endereco.Fecha,
                AbreSabado = dto.Endereco.AbreSabado,
                AbreDomingo = dto.Endereco.AbreDomingo,
                Padrao = true,
                Ativa = true
            };

            ResultadoGeocodificacao local = _geocodificador.Geocodificar(new EnderecoGeocodificacao
            {
                Logradouro = origem.Logradouro,
                Numero = origem.Numero,
                Bairro = origem.Bairro,
                Cidade = origem.Cidade,
                Uf = origem.Uf,
                Cep = origem.Cep
            });
            origem.Latitude = local.Latitude;
            origem.Longitude = local.Longitude;

            _repository.Cadastrar(empresa, admin, origem);

            return new LerEmpresaDto
            {
                Id = empresa.Id,
                Cnpj = empresa.Cnpj,
                NomeFantasia = empresa.NomeFantasia,
                Segmento = empresa.Segmento,
                Status = empresa.Status
            };
        }

        public LerConfiguracaoEmpresaDto ObterConfiguracao(Guid empresaId)
        {
            Empresa empresa = _repository.ObterPorId(empresaId)
                ?? throw new DomainException("Empresa não encontrada.");
            return ParaConfiguracao(empresa);
        }

        public LerConfiguracaoEmpresaDto AtualizarConfiguracao(Guid usuarioLogadoId, AtualizarEmpresaDto dto)
        {
            Usuario admin = PermissaoAdmin.Exigir(_usuarioRepository, usuarioLogadoId,
                "Só o administrador da empresa pode alterar os dados da empresa.");

            Empresa empresa = _repository.ObterPorId(admin.EmpresaId)
                ?? throw new DomainException("Empresa não encontrada.");

            string razaoSocial = (dto.RazaoSocial ?? string.Empty).Trim();
            string nomeFantasia = (dto.NomeFantasia ?? string.Empty).Trim();
            string email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            string? telefone = string.IsNullOrWhiteSpace(dto.Telefone)
                ? null
                : new string(dto.Telefone.Where(char.IsDigit).ToArray());
            string faixa = (dto.FaixaPedidosDia ?? string.Empty).Trim();

            if (razaoSocial.Length == 0 || razaoSocial.Length > 150)
            {
                throw new DomainException("Informe a razão social (até 150 caracteres).");
            }

            if (nomeFantasia.Length == 0 || nomeFantasia.Length > 100)
            {
                throw new DomainException("Informe o nome fantasia (até 100 caracteres).");
            }

            if (email.Length == 0 || email.Length > 255 || !email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
            {
                throw new DomainException("E-mail da empresa inválido.");
            }

            if (telefone != null && (telefone.Length < 10 || telefone.Length > 11))
            {
                throw new DomainException("Telefone inválido.");
            }

            if (!Enum.IsDefined(dto.Segmento))
            {
                throw new DomainException("Segmento inválido.");
            }

            if (faixa.Length == 0 || faixa.Length > 20)
            {
                throw new DomainException("Informe a quantidade de pedidos por dia.");
            }

            if (!dto.EntregaMesmoDia && !dto.EntregaAgendada && !dto.RetiradaLoja)
            {
                throw new DomainException("Escolha ao menos uma forma de entrega.");
            }

            bool fazEntrega = dto.EntregaMesmoDia || dto.EntregaAgendada;
            if (fazEntrega && !dto.UsaCarro && !dto.UsaMoto && !dto.UsaVanCaminhao)
            {
                throw new DomainException("Escolha ao menos um tipo de veículo.");
            }

            if (dto.TempoOfertaFavoritosS < 10 || dto.TempoOfertaFavoritosS > 3600)
            {
                throw new DomainException("O tempo de oferta para favoritos precisa ficar entre 10 segundos e 1 hora.");
            }

            if (dto.MetaNoPrazoPct <= 0 || dto.MetaNoPrazoPct > 100)
            {
                throw new DomainException("A meta de entregas no prazo precisa ficar entre 1% e 100%.");
            }

            empresa.RazaoSocial = razaoSocial;
            empresa.NomeFantasia = nomeFantasia;
            empresa.Email = email;
            empresa.Telefone = telefone;
            empresa.Segmento = dto.Segmento;
            empresa.FaixaPedidosDia = faixa;
            empresa.EntregaMesmoDia = dto.EntregaMesmoDia;
            empresa.EntregaAgendada = dto.EntregaAgendada;
            empresa.RetiradaLoja = dto.RetiradaLoja;
            empresa.UsaCarro = dto.UsaCarro;
            empresa.UsaMoto = dto.UsaMoto;
            empresa.UsaVanCaminhao = dto.UsaVanCaminhao;
            empresa.SomenteFavoritos = dto.SomenteFavoritos;
            empresa.TempoOfertaFavoritosS = dto.TempoOfertaFavoritosS;
            empresa.MetaNoPrazoPct = Math.Round(dto.MetaNoPrazoPct, 2);

            _repository.Atualizar(empresa);
            return ParaConfiguracao(empresa);
        }

        private static LerConfiguracaoEmpresaDto ParaConfiguracao(Empresa e)
        {
            return new LerConfiguracaoEmpresaDto
            {
                Id = e.Id,
                Cnpj = e.Cnpj,
                RazaoSocial = e.RazaoSocial,
                NomeFantasia = e.NomeFantasia,
                Email = e.Email,
                Telefone = e.Telefone,
                Segmento = e.Segmento,
                FaixaPedidosDia = e.FaixaPedidosDia,
                EntregaMesmoDia = e.EntregaMesmoDia,
                EntregaAgendada = e.EntregaAgendada,
                RetiradaLoja = e.RetiradaLoja,
                UsaCarro = e.UsaCarro,
                UsaMoto = e.UsaMoto,
                UsaVanCaminhao = e.UsaVanCaminhao,
                SomenteFavoritos = e.SomenteFavoritos,
                TempoOfertaFavoritosS = e.TempoOfertaFavoritosS,
                MetaNoPrazoPct = e.MetaNoPrazoPct
            };
        }

        // Mesmas regras dos CHECKs do banco, com mensagens legíveis para o front
        private void Validar(CadastrarEmpresaDto dto, string cnpj, string emailAdmin)
        {
            if (cnpj.Length != 14)
            {
                throw new DomainException("CNPJ inválido.");
            }

            if (string.IsNullOrWhiteSpace(dto.RazaoSocial) || string.IsNullOrWhiteSpace(dto.NomeFantasia))
            {
                throw new DomainException("Razão social e nome fantasia são obrigatórios.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new DomainException("E-mail da empresa é obrigatório.");
            }

            if (!Enum.IsDefined(dto.Segmento))
            {
                throw new DomainException("Segmento inválido.");
            }

            if (string.IsNullOrWhiteSpace(dto.FaixaPedidosDia))
            {
                throw new DomainException("Informe a quantidade de pedidos por dia.");
            }

            if (!dto.EntregaMesmoDia && !dto.EntregaAgendada && !dto.RetiradaLoja)
            {
                throw new DomainException("Escolha ao menos uma forma de entrega.");
            }

            bool fazEntrega = dto.EntregaMesmoDia || dto.EntregaAgendada;
            if (fazEntrega && !dto.UsaCarro && !dto.UsaMoto && !dto.UsaVanCaminhao)
            {
                throw new DomainException("Escolha ao menos um tipo de veículo.");
            }

            if (string.IsNullOrWhiteSpace(dto.Responsavel.Nome) || string.IsNullOrWhiteSpace(emailAdmin))
            {
                throw new DomainException("Nome e e-mail do responsável são obrigatórios.");
            }

            if (dto.Responsavel.Senha.Length < 8)
            {
                throw new DomainException("A senha precisa ter pelo menos 8 caracteres.");
            }

            if (!dto.Responsavel.AceitouTermos)
            {
                throw new DomainException("É preciso aceitar os termos de uso.");
            }

            if (dto.Endereco.Fecha <= dto.Endereco.Abre)
            {
                throw new DomainException("O horário de fechamento precisa ser depois da abertura.");
            }

            if (SomenteLetrasENumeros(dto.Endereco.Cep).Length != 8
                || string.IsNullOrWhiteSpace(dto.Endereco.Logradouro)
                || string.IsNullOrWhiteSpace(dto.Endereco.Numero)
                || string.IsNullOrWhiteSpace(dto.Endereco.Bairro)
                || string.IsNullOrWhiteSpace(dto.Endereco.Cidade)
                || dto.Endereco.Uf.Trim().Length != 2)
            {
                throw new DomainException("Endereço incompleto.");
            }

            if (_repository.CnpjExiste(cnpj))
            {
                throw new DomainException("Já existe uma empresa cadastrada com esse CNPJ.");
            }

            if (_usuarioRepository.EmailExiste(emailAdmin))
            {
                throw new DomainException("Já existe um usuário com esse e-mail.");
            }
        }

        private static string SomenteLetrasENumeros(string texto)
        {
            return new string((texto ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
