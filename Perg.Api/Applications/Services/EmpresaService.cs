using Microsoft.AspNetCore.Identity;
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

            // Coordenadas da loja (ponto de partida das rotas)
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
