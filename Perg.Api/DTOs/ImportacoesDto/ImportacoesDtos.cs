using Perg.Api.Domains.Enums;

namespace Perg.Api.DTOs.ImportacoesDto
{
    // multipart/form-data: arquivo (.xlsx ou .csv) + origem (opcional; sem ela, a origem padrão)
    public class EnviarPlanilhaDto
    {
        public IFormFile? Arquivo { get; set; }
        public Guid? OrigemId { get; set; }
    }

    public class ConfirmarImportacaoDto
    {
        // campo do Perg → nome da coluna na planilha. Ex.: { "cliente_nome": "Cliente", "logradouro": "Endereço" }
        public Dictionary<string, string> Mapeamento { get; set; } = new();

        // Usada nas linhas sem coluna de modalidade e sem data/horário
        public ModalidadePedido ModalidadePadrao { get; set; } = ModalidadePedido.MesmoDia;
    }

    public class CampoImportacaoDto
    {
        public string Campo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public bool Obrigatorio { get; set; }
    }

    public class ErroImportacaoDto
    {
        public int Linha { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class LerImportacaoResumoDto
    {
        public Guid Id { get; set; }
        public string ArquivoNome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? TotalLinhas { get; set; }
        public int LinhasImportadas { get; set; }
        public int LinhasComErro { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? ConcluidaEm { get; set; }
    }

    public class LerImportacaoDto : LerImportacaoResumoDto
    {
        public Guid OrigemId { get; set; }
        public Dictionary<string, string>? Mapeamento { get; set; }
        public List<ErroImportacaoDto> Erros { get; set; } = new();

        // Só enquanto aguarda confirmação: o que o front precisa para a tela de "ligar as colunas"
        public List<string>? Colunas { get; set; }
        public Dictionary<string, string>? MapeamentoSugerido { get; set; }
        public List<Dictionary<string, string>>? Amostra { get; set; }
        public List<CampoImportacaoDto>? Campos { get; set; }
    }
}
