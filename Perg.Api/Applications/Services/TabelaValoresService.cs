using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.EmpresasDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Valores pagos aos entregadores: tabela padrão do Perg + ajustes que a loja fizer.
    // Usada para o valor das rotas e pelo otimizador quando a prioridade é "Custo".
    public class TabelaValoresService
    {
        private const decimal MaximoBase = 1000m;
        private const decimal MaximoPorKm = 100m;
        private const decimal MaximoPorEntrega = 100m;

        private readonly IValorVeiculoRepository _repository;
        private readonly IUsuarioRepository _usuarioRepository;

        public TabelaValoresService(IValorVeiculoRepository repository, IUsuarioRepository usuarioRepository)
        {
            _repository = repository;
            _usuarioRepository = usuarioRepository;
        }

        // Tabela que vale para a empresa logada (personalizado por cima do padrão)
        public Dictionary<TipoVeiculo, ValoresVeiculo> ObterTabela()
        {
            var personalizados = _repository.ListarDaEmpresa().ToDictionary(v => v.TipoVeiculo);

            return Enum.GetValues<TipoVeiculo>().ToDictionary(
                tipo => tipo,
                tipo => personalizados.TryGetValue(tipo, out ValorVeiculoEmpresa? v)
                    ? new ValoresVeiculo(v.BaseRota, v.PorKm, v.PorEntrega)
                    : ParametrosVeiculo.ValoresPadrao(tipo));
        }

        public List<LerValorVeiculoDto> Listar()
        {
            var personalizados = _repository.ListarDaEmpresa().ToDictionary(v => v.TipoVeiculo);

            return Enum.GetValues<TipoVeiculo>().Select(tipo =>
            {
                ValoresVeiculo padrao = ParametrosVeiculo.ValoresPadrao(tipo);
                bool temPersonalizado = personalizados.TryGetValue(tipo, out ValorVeiculoEmpresa? v);

                return new LerValorVeiculoDto
                {
                    Tipo = tipo,
                    BaseRota = temPersonalizado ? v!.BaseRota : padrao.BaseRota,
                    PorKm = temPersonalizado ? v!.PorKm : padrao.PorKm,
                    PorEntrega = temPersonalizado ? v!.PorEntrega : padrao.PorEntrega,
                    Personalizado = temPersonalizado,
                    PadraoBaseRota = padrao.BaseRota,
                    PadraoPorKm = padrao.PorKm,
                    PadraoPorEntrega = padrao.PorEntrega
                };
            }).ToList();
        }

        public List<LerValorVeiculoDto> Atualizar(Guid usuarioLogadoId, AtualizarValoresVeiculoDto dto)
        {
            Usuario admin = PermissaoAdmin.Exigir(_usuarioRepository, usuarioLogadoId,
                "Só o administrador da empresa pode alterar os valores pagos aos entregadores.");

            var novos = new List<ValorVeiculoEmpresa>();
            var vistos = new HashSet<TipoVeiculo>();

            foreach (ValorVeiculoDto item in dto.Valores ?? new List<ValorVeiculoDto>())
            {
                if (!Enum.IsDefined(item.Tipo) || !vistos.Add(item.Tipo))
                {
                    throw new DomainException("Tipo de veículo inválido ou repetido.");
                }

                if (item.BaseRota < 0 || item.BaseRota > MaximoBase
                    || item.PorKm < 0 || item.PorKm > MaximoPorKm
                    || item.PorEntrega < 0 || item.PorEntrega > MaximoPorEntrega)
                {
                    throw new DomainException(
                        $"Valores inválidos para {item.Tipo}: base até R$ {MaximoBase:0}, por km e por entrega até R$ {MaximoPorKm:0}.");
                }

                if (item.BaseRota + item.PorKm + item.PorEntrega <= 0)
                {
                    throw new DomainException($"O entregador de {item.Tipo} precisa receber algum valor.");
                }

                var valores = new ValoresVeiculo(Math.Round(item.BaseRota, 2), Math.Round(item.PorKm, 2), Math.Round(item.PorEntrega, 2));

                // Igual ao padrão não precisa guardar: assim, se o Perg mudar o padrão, a loja acompanha
                if (valores == ParametrosVeiculo.ValoresPadrao(item.Tipo))
                {
                    continue;
                }

                novos.Add(new ValorVeiculoEmpresa
                {
                    EmpresaId = admin.EmpresaId,
                    TipoVeiculo = item.Tipo,
                    BaseRota = valores.BaseRota,
                    PorKm = valores.PorKm,
                    PorEntrega = valores.PorEntrega
                });
            }

            _repository.Substituir(admin.EmpresaId, novos);
            return Listar();
        }

        public List<LerValorVeiculoDto> RestaurarPadrao(Guid usuarioLogadoId)
        {
            Usuario admin = PermissaoAdmin.Exigir(_usuarioRepository, usuarioLogadoId,
                "Só o administrador da empresa pode alterar os valores pagos aos entregadores.");

            _repository.Substituir(admin.EmpresaId, new List<ValorVeiculoEmpresa>());
            return Listar();
        }
    }
}
