using Microsoft.AspNetCore.Identity;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.EntregadoresDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;
using Perg.Api.Applications.Comum;

namespace Perg.Api.Applications.Services
{
    public class EntregadorService
    {
        private const string VersaoTermos = "2026-11";

        private readonly IEntregadorRepository _repository;
        private readonly IPasswordHasher<Entregador> _passwordHasher;
        private readonly GeradorTokenJwt _geradorToken;
        private readonly IPosicaoAtualCache _posicoes;

        public EntregadorService(IEntregadorRepository repository, IPasswordHasher<Entregador> passwordHasher, GeradorTokenJwt geradorToken, IPosicaoAtualCache posicoes)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _geradorToken = geradorToken;
            _posicoes = posicoes;
        }

        public LerEntregadorDto Cadastrar(CadastrarEntregadorDto dto)
        {
            string cpf = Formatos.SomenteDigitos(dto.Cpf);
            string telefone = Formatos.SomenteDigitos(dto.Telefone);
            string email = dto.Email.Trim().ToLowerInvariant();
            string? placa = string.IsNullOrWhiteSpace(dto.Veiculo.Placa)
                ? null
                : new string(dto.Veiculo.Placa.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(dto.Nome)) throw new DomainException("Nome é obrigatório.");
            if (!CpfValido(cpf)) throw new DomainException("CPF inválido.");
            if (telefone.Length < 10 || telefone.Length > 11) throw new DomainException("Celular inválido.");
            if (!email.Contains('@')) throw new DomainException("E-mail inválido.");
            if (dto.Senha.Length < 8) throw new DomainException("A senha precisa ter pelo menos 8 caracteres.");
            if (!dto.AceitouTermos) throw new DomainException("É preciso aceitar os termos de uso.");

            if (Formatos.SomenteDigitos(dto.Cep).Length != 8
                || string.IsNullOrWhiteSpace(dto.Logradouro) || string.IsNullOrWhiteSpace(dto.Numero)
                || string.IsNullOrWhiteSpace(dto.Bairro) || string.IsNullOrWhiteSpace(dto.Cidade)
                || dto.Uf.Trim().Length != 2)
            {
                throw new DomainException("Endereço incompleto.");
            }

            if (!Enum.IsDefined(dto.Veiculo.Tipo)) throw new DomainException("Tipo de veículo inválido.");
            if (dto.Veiculo.Tipo != TipoVeiculo.Bike && placa == null) throw new DomainException("Informe a placa do veículo.");
            if (placa != null && placa.Length != 7) throw new DomainException("Placa inválida.");

            if (_repository.CpfExiste(cpf)) throw new DomainException("Já existe um cadastro com esse CPF.");
            if (_repository.TelefoneExiste(telefone)) throw new DomainException("Já existe um cadastro com esse celular.");
            if (_repository.EmailExiste(email)) throw new DomainException("Já existe um cadastro com esse e-mail.");
            if (placa != null && _repository.PlacaExiste(placa)) throw new DomainException("Essa placa já está cadastrada.");

            var entregador = new Entregador
            {
                Id = Guid.CreateVersion7(),
                Nome = dto.Nome.Trim(),
                Cpf = cpf,
                Telefone = telefone,
                Email = email,
                Cep = Formatos.SomenteDigitos(dto.Cep),
                Logradouro = dto.Logradouro.Trim(),
                Numero = dto.Numero.Trim(),
                Complemento = dto.Complemento,
                Bairro = dto.Bairro.Trim(),
                Cidade = dto.Cidade.Trim(),
                Uf = dto.Uf.Trim().ToUpperInvariant(),
                ChavePix = dto.ChavePix,
                TermosVersao = VersaoTermos,
                TermosAceitosEm = DateTime.UtcNow,
                Status = StatusEntregador.EmAnalise
            };
            entregador.SenhaHash = _passwordHasher.HashPassword(entregador, dto.Senha);

            var veiculo = new Veiculo
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = entregador.Id,
                Tipo = dto.Veiculo.Tipo,
                Placa = placa,
                Modelo = dto.Veiculo.Modelo,
                CapacidadeKg = ParametrosVeiculo.CapacidadeKg(dto.Veiculo.Tipo),
                CapacidadeVolumes = ParametrosVeiculo.CapacidadeVolumes(dto.Veiculo.Tipo),
                Ativo = true
            };

            _repository.Cadastrar(entregador, veiculo);
            entregador.VeiculoAtual = veiculo;

            return ParaDto(entregador);
        }

        public string Login(LoginEntregadorDto dto)
        {
            Entregador? entregador = _repository.ObterPorLogin(Formatos.SomenteDigitos(dto.Login));

            if (entregador == null
                || _passwordHasher.VerifyHashedPassword(entregador, entregador.SenhaHash, dto.Senha) == PasswordVerificationResult.Failed)
            {
                throw new DomainException("CPF/celular ou senha inválidos.");
            }

            // Em análise entra, mas só para enviar documentos e ver o perfil (o middleware limita as rotas)
            if (entregador.Status != StatusEntregador.Ativo && entregador.Status != StatusEntregador.EmAnalise)
            {
                throw new DomainException("Seu acesso está desativado. Fale com o suporte.");
            }

            return _geradorToken.GerarTokenEntregador(entregador);
        }

        public LerEntregadorDto ObterPerfil(Guid id)
        {
            Entregador entregador = _repository.ObterPorId(id)
                ?? throw new DomainException("Entregador não encontrado.");

            return ParaDto(entregador);
        }

        public List<LerEntregadorRedeDto> ListarRede(TipoVeiculo? tipo, bool somenteFavoritos, Guid empresaId)
        {
            HashSet<Guid> favoritos = _repository.ListarFavoritosIds().ToHashSet();
            Dictionary<Guid, (long Entregues, long Falhas)> comLoja = _repository.ContarEntregasDaLoja();
            HashSet<Guid> emJornada = _repository.ListarComJornadaAberta();
            Dictionary<Guid, Guid> rotaAtual = _repository.ListarEmpresaDaRotaAtual();
            Origem? origem = _repository.ObterOrigemPadrao();

            return _repository.ListarRede(tipo)
                .Where(e => e.Id.HasValue && (!somenteFavoritos || favoritos.Contains(e.Id.Value)))
                .Select(e =>
                {
                    long entregas = e.TotalEntregas ?? 0;
                    long falhas = e.TotalFalhas ?? 0;
                    (long Entregues, long Falhas) loja = comLoja.GetValueOrDefault(e.Id!.Value);

                    SituacaoEntregadorRede situacao = rotaAtual.TryGetValue(e.Id.Value, out Guid empresaDaRota)
                        ? (empresaDaRota == empresaId ? SituacaoEntregadorRede.EmRota : SituacaoEntregadorRede.OutraLoja)
                        : emJornada.Contains(e.Id.Value) ? SituacaoEntregadorRede.Disponivel : SituacaoEntregadorRede.Offline;

                    return new LerEntregadorRedeDto
                    {
                        Id = e.Id!.Value,
                        Nome = e.Nome ?? string.Empty,
                        FotoChave = e.FotoChave,
                        VeiculoTipo = e.VeiculoTipo == null ? null : ConversorEnumSnakeCase<TipoVeiculo>.ParaEnum(e.VeiculoTipo),
                        VeiculoPlaca = e.VeiculoPlaca,
                        VeiculoVerificado = e.VeiculoVerificado ?? false,
                        CnhVerificada = e.CnhVerificada ?? false,
                        TotalEntregas = entregas,
                        TaxaSucesso = entregas + falhas == 0 ? null : Math.Round((decimal)entregas / (entregas + falhas), 3),
                        NotaMedia = e.NotaMedia,
                        TotalAvaliacoes = e.TotalAvaliacoes ?? 0,
                        TempoAceiteMedioS = e.TempoAceiteMedioS,
                        Favorito = favoritos.Contains(e.Id.Value),
                        EntregasComLoja = loja.Entregues,
                        TaxaSucessoComLoja = loja.Entregues + loja.Falhas == 0
                            ? null
                            : Math.Round((decimal)loja.Entregues / (loja.Entregues + loja.Falhas), 3),
                        Situacao = situacao,
                        DistanciaOrigemKm = situacao == SituacaoEntregadorRede.Offline ? null : DistanciaAteOrigem(e.Id.Value, origem)
                    };
                })
                .ToList();
        }

        // Distância em linha reta (km, 1 casa) da última posição do entregador ao ponto de coleta padrão
        private decimal? DistanciaAteOrigem(Guid entregadorId, Origem? origem)
        {
            if (origem?.Latitude == null || origem.Longitude == null) return null;

            Applications.TempoReal.PosicaoAtual? posicao = _posicoes.Obter(entregadorId);
            if (posicao == null) return null;

            const double RaioTerraKm = 6371;
            double ParaRad(double graus) => graus * Math.PI / 180;
            double lat1 = ParaRad((double)posicao.Latitude), lat2 = ParaRad((double)origem.Latitude.Value);
            double dLat = lat2 - lat1;
            double dLng = ParaRad((double)origem.Longitude.Value - (double)posicao.Longitude);
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLng / 2), 2);
            return Math.Round((decimal)(2 * RaioTerraKm * Math.Asin(Math.Sqrt(a))), 1);
        }

        public void Favoritar(Guid entregadorId, Guid empresaId)
        {
            Entregador entregador = _repository.ObterPorId(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            if (entregador.Status != StatusEntregador.Ativo)
            {
                throw new DomainException("Só é possível favoritar entregadores ativos.");
            }

            if (_repository.ObterFavorito(entregadorId) != null)
            {
                return;
            }

            _repository.AdicionarFavorito(new EmpresaFavorito
            {
                EmpresaId = empresaId,
                EntregadorId = entregadorId
            });
        }

        public void Desfavoritar(Guid entregadorId)
        {
            EmpresaFavorito? favorito = _repository.ObterFavorito(entregadorId);
            if (favorito != null)
            {
                _repository.RemoverFavorito(favorito);
            }
        }

        private static LerEntregadorDto ParaDto(Entregador entregador)
        {
            return new LerEntregadorDto
            {
                Id = entregador.Id,
                Nome = entregador.Nome,
                Telefone = entregador.Telefone,
                Email = entregador.Email,
                Status = entregador.Status,
                TelefoneConfirmado = entregador.TelefoneConfirmado,
                VeiculoTipo = entregador.VeiculoAtual?.Tipo,
                VeiculoPlaca = entregador.VeiculoAtual?.Placa
            };
        }

        private static bool CpfValido(string cpf)
        {
            if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            {
                return false;
            }

            int Digito(int tamanho)
            {
                int soma = 0;
                for (int i = 0; i < tamanho; i++)
                {
                    soma += (cpf[i] - '0') * (tamanho + 1 - i);
                }
                int resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            return Digito(9) == cpf[9] - '0' && Digito(10) == cpf[10] - '0';
        }
    }
}
