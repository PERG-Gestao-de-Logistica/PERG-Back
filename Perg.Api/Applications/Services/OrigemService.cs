using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Domains;
using Perg.Api.DTOs.OrigensDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class OrigemService
    {
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
            string cep = new string((dto.Cep ?? string.Empty).Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(dto.Nome))
            {
                throw new DomainException("Nome da origem é obrigatório.");
            }

            if (cep.Length != 8
                || string.IsNullOrWhiteSpace(dto.Logradouro)
                || string.IsNullOrWhiteSpace(dto.Numero)
                || string.IsNullOrWhiteSpace(dto.Bairro)
                || string.IsNullOrWhiteSpace(dto.Cidade)
                || dto.Uf.Trim().Length != 2)
            {
                throw new DomainException("Endereço incompleto.");
            }

            if (dto.Fecha <= dto.Abre)
            {
                throw new DomainException("O horário de fechamento precisa ser depois da abertura.");
            }

            Origem origem = new Origem
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                ResponsavelId = usuarioId,
                Nome = dto.Nome.Trim(),
                Cep = cep,
                Logradouro = dto.Logradouro.Trim(),
                Numero = dto.Numero.Trim(),
                Complemento = dto.Complemento,
                Bairro = dto.Bairro.Trim(),
                Cidade = dto.Cidade.Trim(),
                Uf = dto.Uf.Trim().ToUpperInvariant(),
                Abre = dto.Abre,
                Fecha = dto.Fecha,
                AbreSabado = dto.AbreSabado,
                AbreDomingo = dto.AbreDomingo,
                Padrao = dto.Padrao,
                Ativa = true
            };

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

            _repository.Adicionar(origem);

            return ParaDto(origem);
        }

        private static LerOrigemDto ParaDto(Origem origem)
        {
            return new LerOrigemDto
            {
                Id = origem.Id,
                Nome = origem.Nome,
                Cep = origem.Cep,
                Logradouro = origem.Logradouro,
                Numero = origem.Numero,
                Complemento = origem.Complemento,
                Bairro = origem.Bairro,
                Cidade = origem.Cidade,
                Uf = origem.Uf,
                Abre = origem.Abre,
                Fecha = origem.Fecha,
                AbreSabado = origem.AbreSabado,
                AbreDomingo = origem.AbreDomingo,
                Padrao = origem.Padrao
            };
        }
    }
}
