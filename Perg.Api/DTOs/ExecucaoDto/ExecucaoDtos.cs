using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.ExecucaoDto
{
    public class LerJornadaDto
    {
        public Guid Id { get; set; }
        public DateTime IniciadaEm { get; set; }
        public DateTime? EncerradaEm { get; set; }
        public TipoVeiculo? VeiculoTipo { get; set; }
    }

    // Rota como o entregador vê no app (com telefone e endereço completo do cliente)
    public class LerRotaAppDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public StatusRota Status { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public string EnderecoColeta { get; set; } = string.Empty;
        public decimal? ColetaLatitude { get; set; }
        public decimal? ColetaLongitude { get; set; }
        public DateTime? ColetaPrevistaEm { get; set; }
        public decimal? ValorEntregador { get; set; }
        public int? DistanciaPrevistaM { get; set; }
        public int? DuracaoPrevistaS { get; set; }
        public string? Trajeto { get; set; }
        public List<LerParadaAppDto> Paradas { get; set; } = new();
    }

    public class LerParadaAppDto
    {
        public Guid Id { get; set; }
        public short Sequencia { get; set; }
        public StatusParada Status { get; set; }
        public DateTime? ChegadaPrevista { get; set; }
        public int NumeroPedido { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string? ClienteTelefone { get; set; }
        public string Endereco { get; set; } = string.Empty;
        public string? Complemento { get; set; }
        public string? Referencia { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int Volumes { get; set; }
        public string? Observacoes { get; set; }
        public DateTime? JanelaInicio { get; set; }
        public DateTime? JanelaFim { get; set; }
    }

    public class ConcluirEntregaDto
    {
        public TipoComprovante TipoComprovante { get; set; }

        // PIN de 4 dígitos que o cliente vê no rastreio (quando TipoComprovante = Pin)
        public string? Pin { get; set; }

        // Caminho da foto/assinatura no armazenamento (quando TipoComprovante = Foto ou Assinatura)
        public string? ComprovanteChave { get; set; }

        public string? RecebedorNome { get; set; }

        // GPS do celular no momento do "Confirmar"
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class RegistrarOcorrenciaDto
    {
        public TipoOcorrencia Tipo { get; set; }
        public string? Descricao { get; set; }
        public string? FotoChave { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class LerCarteiraDto
    {
        public decimal Saldo { get; set; }
        public List<LerLancamentoDto> Lancamentos { get; set; } = new();
    }

    public class LerLancamentoDto
    {
        public TipoLancamento Tipo { get; set; }
        public decimal Valor { get; set; }
        public string? Descricao { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
