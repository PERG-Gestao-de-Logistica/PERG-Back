using System.Text.Json;
using System.Text.Json.Serialization;
using Perg.Api.Applications.Importacoes;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.ImportacoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    // Importação de pedidos por planilha:
    // 1. Enviar: guarda o arquivo, lê o cabeçalho e sugere qual coluna é qual campo → "aguardando_confirmacao"
    // 2. Confirmar: o lojista ajusta o mapeamento → "processando"
    // 3. ProcessadorImportacoes (segundo plano) cria os pedidos linha a linha (com geocodificação) → "concluida"
    public class ImportacaoService
    {
        private const long TamanhoMaximo = 5 * 1024 * 1024;
        private const int LinhasAmostra = 5;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IImportacaoRepository _repository;
        private readonly IOrigemRepository _origemRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IArmazenamentoArquivos _armazenamento;
        private readonly PedidoService _pedidoService;
        private readonly NotificadorTempoReal _tempoReal;

        public ImportacaoService(IImportacaoRepository repository, IOrigemRepository origemRepository,
            IEmpresaRepository empresaRepository, IArmazenamentoArquivos armazenamento,
            PedidoService pedidoService, NotificadorTempoReal tempoReal)
        {
            _repository = repository;
            _origemRepository = origemRepository;
            _empresaRepository = empresaRepository;
            _armazenamento = armazenamento;
            _pedidoService = pedidoService;
            _tempoReal = tempoReal;
        }

        // ---------- 1. Enviar ----------

        public LerImportacaoDto Enviar(EnviarPlanilhaDto dto, Guid empresaId, Guid usuarioId)
        {
            IFormFile arquivo = dto.Arquivo ?? throw new DomainException("Envie a planilha no campo \"arquivo\".");

            if (arquivo.Length == 0) throw new DomainException("O arquivo está vazio.");
            if (arquivo.Length > TamanhoMaximo) throw new DomainException("A planilha pode ter no máximo 5 MB.");

            string extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (extensao != ".xlsx" && extensao != ".csv")
            {
                throw new DomainException("Formato não aceito. Envie .xlsx (Excel) ou .csv. Se for .xls antigo, salve como .xlsx.");
            }

            using var memoria = new MemoryStream();
            arquivo.CopyTo(memoria);
            byte[] bytes = memoria.ToArray();
            ConferirConteudo(bytes, extensao);

            Origem origem = (dto.OrigemId.HasValue ? _origemRepository.ObterPorId(dto.OrigemId.Value) : _origemRepository.ObterPadrao())
                ?? throw new DomainException("Origem não encontrada.");
            if (!origem.Ativa) throw new DomainException("Origem não encontrada.");

            PlanilhaLida planilha = LeitorPlanilha.Ler(new MemoryStream(bytes), extensao);
            Dictionary<string, string> sugestao = CamposImportacao.Sugerir(planilha.Cabecalhos);

            string chave = _armazenamento.Salvar(new MemoryStream(bytes), $"importacoes/{empresaId}", extensao.TrimStart('.'));

            string nome = Path.GetFileName(arquivo.FileName);
            var importacao = new Importacao
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                OrigemId = origem.Id,
                ArquivoNome = nome.Length > 255 ? nome[..255] : nome,
                ArquivoChave = chave,
                Status = StatusImportacao.AguardandoConfirmacao,
                TotalLinhas = planilha.Linhas.Count,
                MapeamentoColunas = Serializar(new ConfiguracaoMapeamento { Campos = sugestao })
            };
            _repository.Adicionar(importacao);

            return ParaDto(importacao, planilha);
        }

        // ---------- consultas ----------

        public List<LerImportacaoResumoDto> Listar()
        {
            return _repository.Listar(30).Select(i => PreencherResumo(new LerImportacaoResumoDto(), i)).ToList();
        }

        public LerImportacaoDto Obter(Guid id)
        {
            Importacao importacao = _repository.ObterPorId(id) ?? throw new DomainException("Importação não encontrada.");
            PlanilhaLida? planilha = importacao.Status == StatusImportacao.AguardandoConfirmacao ? LerArquivo(importacao) : null;
            return ParaDto(importacao, planilha);
        }

        // ---------- 2. Confirmar ----------

        public LerImportacaoDto Confirmar(Guid id, ConfirmarImportacaoDto dto)
        {
            Importacao importacao = _repository.ObterPorId(id) ?? throw new DomainException("Importação não encontrada.");

            if (importacao.Status != StatusImportacao.AguardandoConfirmacao)
            {
                throw new DomainException("Esta importação já foi confirmada.");
            }

            if (!Enum.IsDefined(dto.ModalidadePadrao))
            {
                throw new DomainException("Modalidade padrão inválida.");
            }

            PlanilhaLida planilha = LerArquivo(importacao);

            var mapeamento = new Dictionary<string, string>();
            foreach (var (campo, coluna) in dto.Mapeamento ?? new())
            {
                if (string.IsNullOrWhiteSpace(coluna)) continue; // campo deixado sem coluna

                if (!CamposImportacao.Existe(campo))
                {
                    throw new DomainException($"Campo \"{campo}\" não existe.");
                }

                if (!planilha.Cabecalhos.Contains(coluna))
                {
                    throw new DomainException($"A coluna \"{coluna}\" não está na planilha.");
                }

                mapeamento[campo] = coluna;
            }

            bool soRetirada = dto.ModalidadePadrao == ModalidadePedido.Retirada && !mapeamento.ContainsKey(CamposImportacao.Modalidade);
            var faltando = CamposImportacao.Todos
                .Where(c => c.Obrigatorio && !mapeamento.ContainsKey(c.Campo))
                .Where(c => !soRetirada || c.Campo == CamposImportacao.ClienteNome)
                .Select(c => c.Descricao)
                .ToList();

            if (faltando.Count > 0)
            {
                throw new DomainException("Escolha a coluna de: " + string.Join(", ", faltando) + ".");
            }

            importacao.MapeamentoColunas = Serializar(new ConfiguracaoMapeamento { Campos = mapeamento, ModalidadePadrao = dto.ModalidadePadrao });
            importacao.Status = StatusImportacao.Processando;
            importacao.TotalLinhas = planilha.Linhas.Count;
            importacao.LinhasImportadas = 0;
            importacao.LinhasComErro = 0;
            importacao.Erros = null;
            _repository.Salvar();

            return ParaDto(importacao, null);
        }

        // ---------- 3. Processamento (chamado pelo ProcessadorImportacoes) ----------

        // Pega a próxima importação "processando" e prepara as linhas que faltam (retoma de onde parou se a API reiniciou)
        public PlanoImportacao? PrepararProxima()
        {
            Importacao? importacao = _repository.ObterProximaParaProcessar();
            if (importacao == null) return null;

            try
            {
                PlanilhaLida planilha = LerArquivo(importacao);
                ConfiguracaoMapeamento config = Desserializar(importacao.MapeamentoColunas);
                Empresa empresa = _empresaRepository.ObterPorId(importacao.EmpresaId)
                    ?? throw new DomainException("Empresa não encontrada.");

                int jaProcessadas = importacao.LinhasImportadas + importacao.LinhasComErro;

                return new PlanoImportacao
                {
                    ImportacaoId = importacao.Id,
                    EmpresaId = importacao.EmpresaId,
                    UsuarioId = importacao.UsuarioId,
                    Conversor = new ConversorLinha(config.Campos, planilha.Cabecalhos, config.ModalidadePadrao,
                        TimeZoneInfo.FindSystemTimeZoneById(empresa.FusoHorario), importacao.OrigemId),
                    Cabecalhos = planilha.Cabecalhos,
                    Linhas = planilha.Linhas.Skip(jaProcessadas).ToList(),
                    Importadas = importacao.LinhasImportadas,
                    Erros = LerErros(importacao.Erros)
                };
            }
            catch (DomainException ex)
            {
                importacao.Status = StatusImportacao.Falhou;
                importacao.Erros = Serializar(new List<ErroImportacaoDto> { new() { Linha = 0, Motivo = ex.Message } });
                importacao.ConcluidaEm = DateTime.UtcNow;
                Notificar(importacao, "Importação falhou: " + ex.Message);
                return null;
            }
        }

        // Cria o pedido de uma linha. Erro de regra (DomainException) = linha com erro; o resto segue.
        public void ImportarLinha(PlanoImportacao plano, LinhaPlanilha linha)
        {
            var dados = new Dictionary<string, string>();
            for (int i = 0; i < plano.Cabecalhos.Count && i < linha.Valores.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(linha.Valores[i])) dados[plano.Cabecalhos[i]] = linha.Valores[i];
            }

            var pedido = plano.Conversor.Converter(linha.Valores);
            _pedidoService.CriarDaPlanilha(pedido, plano.EmpresaId, plano.UsuarioId, plano.ImportacaoId, JsonSerializer.Serialize(dados));
        }

        public void RegistrarProgresso(PlanoImportacao plano, bool concluir)
        {
            Importacao importacao = _repository.ObterPorId(plano.ImportacaoId)
                ?? throw new DomainException("Importação não encontrada.");

            importacao.LinhasImportadas = plano.Importadas;
            importacao.LinhasComErro = plano.Erros.Count;
            importacao.Erros = plano.Erros.Count > 0 ? Serializar(plano.Erros) : null;

            if (concluir)
            {
                importacao.Status = StatusImportacao.Concluida;
                importacao.ConcluidaEm = DateTime.UtcNow;
                string titulo = plano.Erros.Count == 0
                    ? $"Importação concluída: {plano.Importadas} pedido(s) importado(s)"
                    : $"Importação concluída: {plano.Importadas} pedido(s) importado(s), {plano.Erros.Count} linha(s) com erro";
                Notificar(importacao, titulo);
                return;
            }

            _repository.Salvar();
        }

        // ---------- apoio ----------

        // Salva junto com a importação e avisa em tempo real quem enviou a planilha
        private void Notificar(Importacao importacao, string titulo)
        {
            var notificacao = new Notificacao
            {
                Id = Guid.CreateVersion7(),
                EmpresaId = importacao.EmpresaId,
                UsuarioId = importacao.UsuarioId,
                Tipo = TipoNotificacao.Importacao,
                Titulo = titulo.Length > 200 ? titulo[..200] : titulo,
                Link = $"/importacoes/{importacao.Id}",
                CriadoEm = DateTime.UtcNow
            };
            _repository.AdicionarNotificacao(notificacao);
            _repository.Salvar();
            _tempoReal.EnviarNotificacoes([notificacao]);
        }

        private PlanilhaLida LerArquivo(Importacao importacao)
        {
            using Stream arquivo = _armazenamento.Abrir(importacao.ArquivoChave)
                ?? throw new DomainException("O arquivo da planilha não foi encontrado.");
            return LeitorPlanilha.Ler(arquivo, Path.GetExtension(importacao.ArquivoChave).ToLowerInvariant());
        }

        // .xlsx é um zip ("PK"); .csv não pode ter bytes nulos (seria um binário renomeado)
        private static void ConferirConteudo(byte[] bytes, string extensao)
        {
            bool valido = extensao == ".xlsx"
                ? bytes.Length > 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04
                : !bytes.Take(4096).Contains((byte)0);

            if (!valido)
            {
                throw new DomainException("O conteúdo do arquivo não corresponde a uma planilha " + extensao + ".");
            }
        }

        private static LerImportacaoDto ParaDto(Importacao i, PlanilhaLida? planilha)
        {
            ConfiguracaoMapeamento config = Desserializar(i.MapeamentoColunas);
            var dto = PreencherResumo(new LerImportacaoDto(), i);
            dto.OrigemId = i.OrigemId;
            dto.Erros = LerErros(i.Erros);

            if (planilha == null)
            {
                dto.Mapeamento = config.Campos;
                return dto;
            }

            dto.Colunas = planilha.Cabecalhos;
            dto.MapeamentoSugerido = config.Campos;
            dto.Campos = CamposImportacao.Todos
                .Select(c => new CampoImportacaoDto { Campo = c.Campo, Descricao = c.Descricao, Obrigatorio = c.Obrigatorio })
                .ToList();
            dto.Amostra = planilha.Linhas.Take(LinhasAmostra)
                .Select(l => planilha.Cabecalhos
                    .Select((coluna, indice) => (coluna, valor: indice < l.Valores.Length ? l.Valores[indice] : string.Empty))
                    .ToDictionary(x => x.coluna, x => x.valor))
                .ToList();
            return dto;
        }

        private static T PreencherResumo<T>(T dto, Importacao i) where T : LerImportacaoResumoDto
        {
            dto.Id = i.Id;
            dto.ArquivoNome = i.ArquivoNome;
            dto.Status = i.Status;
            dto.TotalLinhas = i.TotalLinhas;
            dto.LinhasImportadas = i.LinhasImportadas;
            dto.LinhasComErro = i.LinhasComErro;
            dto.CriadoEm = i.CriadoEm;
            dto.ConcluidaEm = i.ConcluidaEm;
            return dto;
        }

        private static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Json);

        private static ConfiguracaoMapeamento Desserializar(string? json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? new ConfiguracaoMapeamento()
                : JsonSerializer.Deserialize<ConfiguracaoMapeamento>(json, Json) ?? new ConfiguracaoMapeamento();
        }

        private static List<ErroImportacaoDto> LerErros(string? json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? new List<ErroImportacaoDto>()
                : JsonSerializer.Deserialize<List<ErroImportacaoDto>>(json, Json) ?? new List<ErroImportacaoDto>();
        }

        // O que fica em importacoes.mapeamento_colunas
        private class ConfiguracaoMapeamento
        {
            public Dictionary<string, string> Campos { get; set; } = new();
            public ModalidadePedido ModalidadePadrao { get; set; } = ModalidadePedido.MesmoDia;
        }
    }

    // Estado de uma importação em andamento (vive só enquanto o processador trabalha nela)
    public class PlanoImportacao
    {
        public Guid ImportacaoId { get; set; }
        public Guid EmpresaId { get; set; }
        public Guid UsuarioId { get; set; }
        public ConversorLinha Conversor { get; set; } = null!;
        public List<string> Cabecalhos { get; set; } = new();
        public List<LinhaPlanilha> Linhas { get; set; } = new();
        public int Importadas { get; set; }
        public List<ErroImportacaoDto> Erros { get; set; } = new();
    }
}
