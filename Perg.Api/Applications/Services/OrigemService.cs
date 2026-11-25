using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.OrigensDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class OrigemService
    {
        // Bits dos dias: 1 dom, 2 seg, 4 ter, 8 qua, 16 qui, 32 sex, 64 sáb
        private const short SegASex = 62;
        private const short BitSabado = 64;
        private const short BitDomingo = 1;

        private readonly IOrigemRepository _repository;
        private readonly IGeocodificador _geocodificador;

        public OrigemService(IOrigemRepository repository, IGeocodificador geocodificador)
        {
            _repository = repository;
            _geocodificador = geocodificador;
        }

        public List<LerOrigemDto> Listar()
        {
            return _repository.Listar().Select(ParaDto).ToList();
        }

        public LerOrigemDto Criar(CriarOrigemDto dto, Guid empresaId, Guid usuarioId)
        {
            Origem origem = new Origem
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                ResponsavelId = usuarioId,
                Ativa = true
            };

            Aplicar(origem, dto);
            DefinirLocal(origem, dto, enderecoMudou: true);

            _repository.Adicionar(origem);
            return ParaDto(origem);
        }

        public LerOrigemDto Atualizar(Guid id, CriarOrigemDto dto)
        {
            Origem origem = ObterAtiva(id);

            bool padraoAntes = origem.Padrao;
            string enderecoAntes = TextoEndereco(origem);

            Aplicar(origem, dto);

            // A origem padrão só deixa de ser quando outra é marcada no lugar
            if (padraoAntes && !origem.Padrao)
            {
                origem.Padrao = true;
            }

            DefinirLocal(origem, dto, enderecoMudou: TextoEndereco(origem) != enderecoAntes);

            _repository.Atualizar(origem);
            return ParaDto(origem);
        }

        public LerOrigemDto DefinirPadrao(Guid id)
        {
            Origem origem = ObterAtiva(id);
            origem.Padrao = true;
            _repository.Atualizar(origem);
            return ParaDto(origem);
        }

        // Endereço -> ponto no mapa, para o modal mostrar antes de salvar
        public ResultadoGeocodificacao Localizar(LocalizarOrigemDto dto)
        {
            string cep = SomenteDigitos(dto.Cep);
            if (cep.Length != 8 || string.IsNullOrWhiteSpace(dto.Logradouro) || string.IsNullOrWhiteSpace(dto.Cidade))
            {
                throw new DomainException("Preencha CEP, rua e cidade para localizar no mapa.");
            }

            return _geocodificador.Geocodificar(new EnderecoGeocodificacao
            {
                Logradouro = dto.Logradouro.Trim(),
                Numero = dto.Numero?.Trim(),
                Bairro = dto.Bairro?.Trim(),
                Cidade = dto.Cidade.Trim(),
                Uf = dto.Uf?.Trim().ToUpperInvariant(),
                Cep = cep
            });
        }

        private Origem ObterAtiva(Guid id)
        {
            Origem? origem = _repository.ObterPorId(id);
            if (origem == null || !origem.Ativa)
            {
                throw new DomainException("Origem não encontrada.");
            }
            return origem;
        }

        // Valida o formulário e copia para a origem (criar e editar usam as mesmas regras)
        private static void Aplicar(Origem origem, CriarOrigemDto dto)
        {
            string nome = (dto.Nome ?? string.Empty).Trim();
            string cep = SomenteDigitos(dto.Cep);
            string? telefone = string.IsNullOrWhiteSpace(dto.ResponsavelTelefone) ? null : SomenteDigitos(dto.ResponsavelTelefone);
            string? responsavel = string.IsNullOrWhiteSpace(dto.ResponsavelNome) ? null : dto.ResponsavelNome.Trim();
            string? instrucoes = string.IsNullOrWhiteSpace(dto.InstrucoesColeta) ? null : dto.InstrucoesColeta.Trim();
            string? complemento = string.IsNullOrWhiteSpace(dto.Complemento) ? null : dto.Complemento.Trim();

            if (nome.Length == 0 || nome.Length > 100)
            {
                throw new DomainException("Informe o nome da origem (até 100 caracteres).");
            }

            if (!Enum.IsDefined(dto.Tipo))
            {
                throw new DomainException("Tipo de origem inválido.");
            }

            if (cep.Length != 8
                || string.IsNullOrWhiteSpace(dto.Logradouro)
                || string.IsNullOrWhiteSpace(dto.Numero)
                || string.IsNullOrWhiteSpace(dto.Bairro)
                || string.IsNullOrWhiteSpace(dto.Cidade)
                || (dto.Uf ?? string.Empty).Trim().Length != 2)
            {
                throw new DomainException("Endereço incompleto.");
            }

            if (dto.Numero.Trim().Length > 10)
            {
                throw new DomainException("O número pode ter até 10 caracteres.");
            }

            if (responsavel != null && responsavel.Length > 150)
            {
                throw new DomainException("O nome do responsável pode ter até 150 caracteres.");
            }

            if (telefone != null && (telefone.Length < 10 || telefone.Length > 11))
            {
                throw new DomainException("Telefone do responsável inválido.");
            }

            if (instrucoes != null && instrucoes.Length > 300)
            {
                throw new DomainException("As instruções de coleta podem ter até 300 caracteres.");
            }

            if (dto.Fecha <= dto.Abre)
            {
                throw new DomainException("O horário de fechamento precisa ser depois da abertura.");
            }

            short dias = MontarDias(dto);
            if (dias == 0)
            {
                throw new DomainException("Escolha ao menos um dia de funcionamento.");
            }

            origem.Nome = nome;
            origem.Tipo = dto.Tipo;
            origem.Cep = cep;
            origem.Logradouro = dto.Logradouro.Trim();
            origem.Numero = dto.Numero.Trim();
            origem.Complemento = complemento;
            origem.Bairro = dto.Bairro.Trim();
            origem.Cidade = dto.Cidade.Trim();
            origem.Uf = dto.Uf!.Trim().ToUpperInvariant();
            origem.ResponsavelNome = responsavel;
            origem.ResponsavelTelefone = telefone;
            origem.Abre = dto.Abre;
            origem.Fecha = dto.Fecha;
            origem.DiasFuncionamento = dias;
            // Mantidos em sincronia para o código que ainda lê os campos antigos
            origem.AbreSabado = (dias & BitSabado) != 0;
            origem.AbreDomingo = (dias & BitDomingo) != 0;
            origem.InstrucoesColeta = instrucoes;
            origem.Padrao = dto.Padrao;
        }

        // Ponto no mapa: o que veio do mapa (arrastado) tem prioridade; senão, calcula pelo endereço
        private void DefinirLocal(Origem origem, CriarOrigemDto dto, bool enderecoMudou)
        {
            if (dto.Latitude.HasValue && dto.Longitude.HasValue)
            {
                if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
                {
                    throw new DomainException("Ponto do mapa inválido.");
                }
                origem.Latitude = Math.Round(dto.Latitude.Value, 6);
                origem.Longitude = Math.Round(dto.Longitude.Value, 6);
                return;
            }

            if (!enderecoMudou && origem.Latitude.HasValue)
            {
                return;
            }

            // A origem é o ponto de partida das rotas, então precisa de coordenadas
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
        }

        private static short MontarDias(CriarOrigemDto dto)
        {
            if (dto.Dias == null)
            {
                return (short)(SegASex
                    + (dto.AbreSabado ? BitSabado : 0)
                    + (dto.AbreDomingo ? BitDomingo : 0));
            }

            int bits = 0;
            foreach (int dia in dto.Dias)
            {
                if (dia < 0 || dia > 6)
                {
                    throw new DomainException("Dia de funcionamento inválido.");
                }
                bits |= 1 << dia;
            }
            return (short)bits;
        }

        private static List<int> ListarDias(short bits)
        {
            return Enumerable.Range(0, 7).Where(d => (bits & (1 << d)) != 0).ToList();
        }

        private static string TextoEndereco(Origem o)
        {
            return $"{o.Cep}|{o.Logradouro}|{o.Numero}|{o.Bairro}|{o.Cidade}|{o.Uf}";
        }

        private static string SomenteDigitos(string? texto)
        {
            return new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private static LerOrigemDto ParaDto(Origem origem)
        {
            return new LerOrigemDto
            {
                Id = origem.Id,
                Nome = origem.Nome,
                Tipo = origem.Tipo,
                Cep = origem.Cep,
                Logradouro = origem.Logradouro,
                Numero = origem.Numero,
                Complemento = origem.Complemento,
                Bairro = origem.Bairro,
                Cidade = origem.Cidade,
                Uf = origem.Uf,
                Latitude = origem.Latitude,
                Longitude = origem.Longitude,
                ResponsavelNome = origem.ResponsavelNome,
                ResponsavelTelefone = origem.ResponsavelTelefone,
                Abre = origem.Abre,
                Fecha = origem.Fecha,
                Dias = ListarDias(origem.DiasFuncionamento),
                AbreSabado = origem.AbreSabado,
                AbreDomingo = origem.AbreDomingo,
                InstrucoesColeta = origem.InstrucoesColeta,
                Padrao = origem.Padrao
            };
        }
    }
}
