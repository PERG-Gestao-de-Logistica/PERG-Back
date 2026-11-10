using System.Globalization;
using System.Text;

namespace Perg.Api.Applications.Importacoes
{
    public record CampoImportacao(string Campo, string Descricao, bool Obrigatorio, string[] Sinonimos);

    // Campos do pedido que a planilha pode preencher e os nomes de coluna que reconhecemos sozinhos.
    // A sugestão compara o cabeçalho "normalizado" (sem acento, espaço e pontuação, em minúsculas).
    public static class CamposImportacao
    {
        public const string NumeroExterno = "numero_externo";
        public const string ClienteNome = "cliente_nome";
        public const string ClienteTelefone = "cliente_telefone";
        public const string ClienteEmail = "cliente_email";
        public const string Cep = "cep";
        public const string Logradouro = "logradouro";
        public const string Numero = "numero";
        public const string Complemento = "complemento";
        public const string Bairro = "bairro";
        public const string Cidade = "cidade";
        public const string Uf = "uf";
        public const string Referencia = "referencia";
        public const string Modalidade = "modalidade";
        public const string DataEntrega = "data_entrega";
        public const string JanelaInicio = "janela_inicio";
        public const string JanelaFim = "janela_fim";
        public const string PesoKg = "peso_kg";
        public const string Volumes = "volumes";
        public const string Observacoes = "observacoes";

        public static readonly IReadOnlyList<CampoImportacao> Todos =
        [
            new(NumeroExterno, "Número do pedido na loja (evita importar o mesmo pedido duas vezes)", false,
                ["pedido", "numeropedido", "npedido", "nopedido", "numerodopedido", "codigopedido", "codpedido", "idpedido", "codigo", "ordem", "order", "orderid", "venda", "numerovenda"]),
            new(ClienteNome, "Nome do cliente", true,
                ["cliente", "nome", "nomecliente", "nomedocliente", "destinatario", "nomedestinatario", "comprador", "nomecompleto"]),
            new(ClienteTelefone, "Telefone/WhatsApp do cliente", false,
                ["telefone", "celular", "fone", "whatsapp", "tel", "telefonecliente", "contato", "telefonecelular"]),
            new(ClienteEmail, "E-mail do cliente", false, ["email", "emailcliente", "emaildocliente"]),
            new(Cep, "CEP", false, ["cep", "codigopostal"]),
            new(Logradouro, "Rua/avenida (pode vir com o número junto)", true,
                ["logradouro", "endereco", "rua", "avenida", "enderecoentrega", "enderecodeentrega", "endereco1"]),
            new(Numero, "Número do endereço", false, ["numero", "num", "n", "no", "nro"]),
            new(Complemento, "Complemento", false, ["complemento", "compl", "apto", "apartamento"]),
            new(Bairro, "Bairro", false, ["bairro", "distrito"]),
            new(Cidade, "Cidade", true, ["cidade", "municipio", "localidade"]),
            new(Uf, "Estado (sigla, ex.: SP)", true, ["uf", "estado", "siglaestado"]),
            new(Referencia, "Ponto de referência", false, ["referencia", "pontoreferencia", "pontodereferencia"]),
            new(Modalidade, "Mesmo dia, agendada ou retirada", false, ["modalidade", "tipoentrega", "tipodeentrega", "tipo", "entrega"]),
            new(DataEntrega, "Data da entrega (dd/mm/aaaa)", false,
                ["data", "dataentrega", "datadeentrega", "dataagendada", "dataagendamento", "diaentrega"]),
            new(JanelaInicio, "Início da janela (hh:mm)", false,
                ["horainicio", "inicio", "janelainicio", "horarioinicio", "de", "apartirde", "horainicial"]),
            new(JanelaFim, "Fim da janela (hh:mm)", false,
                ["horafim", "fim", "janelafim", "horariofim", "ate", "horafinal", "horalimite"]),
            new(PesoKg, "Peso em kg", false, ["peso", "pesokg", "kg"]),
            new(Volumes, "Quantidade de volumes", false,
                ["volumes", "volume", "qtdvolumes", "quantidadevolumes", "qtdevolumes", "caixas", "pacotes"]),
            new(Observacoes, "Observações para a entrega", false,
                ["observacoes", "observacao", "obs", "notas", "comentarios", "instrucoes"])
        ];

        public static bool Existe(string campo) => Todos.Any(c => c.Campo == campo);

        // campo → nome da coluna da planilha (só os que reconheceu)
        public static Dictionary<string, string> Sugerir(List<string> cabecalhos)
        {
            var sugestao = new Dictionary<string, string>();
            var colunasUsadas = new HashSet<string>();

            foreach (CampoImportacao campo in Todos)
            {
                string? coluna = cabecalhos.FirstOrDefault(c =>
                    !colunasUsadas.Contains(c) && campo.Sinonimos.Contains(Normalizar(c)));

                if (coluna != null)
                {
                    sugestao[campo.Campo] = coluna;
                    colunasUsadas.Add(coluna);
                }
            }

            return sugestao;
        }

        // "Nº do Pedido" → "nodopedido"; "E-mail" → "email"
        public static string Normalizar(string texto)
        {
            var sb = new StringBuilder();
            foreach (char c in texto.Normalize(NormalizationForm.FormKD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
