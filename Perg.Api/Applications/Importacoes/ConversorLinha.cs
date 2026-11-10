using System.Globalization;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.PedidosDto;
using Perg.Api.Exceptions;

namespace Perg.Api.Applications.Importacoes
{
    // Transforma uma linha da planilha num CriarPedidoDto (as regras do pedido são conferidas depois, no PedidoService)
    public class ConversorLinha
    {
        private static readonly CultureInfo PtBr = new("pt-BR");
        private static readonly string[] FormatosData = ["dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yyyy"];
        private static readonly string[] FormatosDataHora = ["dd/MM/yyyy HH:mm", "d/M/yyyy H:mm", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm:ss"];
        private static readonly string[] FormatosHora = ["HH:mm", "H:mm", "HH:mm:ss", "HH'h'mm", "H'h'mm", "HH'h'", "H'h'"];

        private readonly Dictionary<string, int> _indices; // campo → índice da coluna
        private readonly ModalidadePedido _modalidadePadrao;
        private readonly TimeZoneInfo _fuso;
        private readonly Guid _origemId;

        public ConversorLinha(Dictionary<string, string> mapeamento, List<string> cabecalhos,
            ModalidadePedido modalidadePadrao, TimeZoneInfo fuso, Guid origemId)
        {
            _indices = mapeamento
                .Where(m => cabecalhos.Contains(m.Value))
                .ToDictionary(m => m.Key, m => cabecalhos.IndexOf(m.Value));
            _modalidadePadrao = modalidadePadrao;
            _fuso = fuso;
            _origemId = origemId;
        }

        public CriarPedidoDto Converter(string[] valores)
        {
            var dto = new CriarPedidoDto
            {
                OrigemId = _origemId,
                NumeroExterno = Valor(valores, CamposImportacao.NumeroExterno),
                ClienteNome = Valor(valores, CamposImportacao.ClienteNome) ?? string.Empty,
                ClienteTelefone = Valor(valores, CamposImportacao.ClienteTelefone),
                ClienteEmail = Valor(valores, CamposImportacao.ClienteEmail),
                Cep = Cep(Valor(valores, CamposImportacao.Cep)),
                Logradouro = Valor(valores, CamposImportacao.Logradouro),
                Numero = Valor(valores, CamposImportacao.Numero),
                Complemento = Valor(valores, CamposImportacao.Complemento),
                Bairro = Valor(valores, CamposImportacao.Bairro),
                Cidade = Valor(valores, CamposImportacao.Cidade),
                Uf = Valor(valores, CamposImportacao.Uf),
                Referencia = Valor(valores, CamposImportacao.Referencia),
                Observacoes = Valor(valores, CamposImportacao.Observacoes),
                PesoKg = Peso(Valor(valores, CamposImportacao.PesoKg)),
                Volumes = Volumes(Valor(valores, CamposImportacao.Volumes))
            };

            if (dto.NumeroExterno?.Length > 50)
            {
                throw new DomainException("Número do pedido muito longo (máximo 50 caracteres).");
            }

            // Janela: data + horas no fuso da loja
            string? data = Valor(valores, CamposImportacao.DataEntrega);
            string? inicio = Valor(valores, CamposImportacao.JanelaInicio);
            string? fim = Valor(valores, CamposImportacao.JanelaFim);

            ModalidadePedido? modalidadeInformada = Modalidade(Valor(valores, CamposImportacao.Modalidade));
            bool temJanela = data != null || inicio != null || fim != null;
            dto.Modalidade = modalidadeInformada ?? (temJanela ? ModalidadePedido.Agendada : _modalidadePadrao);

            if (temJanela && dto.Modalidade != ModalidadePedido.Retirada)
            {
                (dto.JanelaInicio, dto.JanelaFim) = Janela(data, inicio, fim, dto.Modalidade);
            }

            return dto;
        }

        private string? Valor(string[] valores, string campo)
        {
            if (!_indices.TryGetValue(campo, out int indice) || indice >= valores.Length) return null;
            string texto = valores[indice].Trim();
            return texto.Length == 0 ? null : texto;
        }

        private static string? Cep(string? texto)
        {
            if (texto == null) return null;
            string digitos = new string(texto.Where(char.IsDigit).ToArray());
            // CEP salvo como número no Excel perde o zero da frente (01310100 → 1310100)
            return digitos.Length == 7 ? "0" + digitos : texto;
        }

        private static decimal Peso(string? texto)
        {
            if (texto == null) return 0m;
            string limpo = texto.ToLowerInvariant().Replace("kg", "").Trim();
            if (decimal.TryParse(limpo, NumberStyles.Number, PtBr, out decimal valor) && !limpo.Contains('.')) return valor;
            if (decimal.TryParse(limpo, NumberStyles.Number, CultureInfo.InvariantCulture, out valor)) return valor;
            throw new DomainException($"Peso inválido: \"{texto}\".");
        }

        private static int Volumes(string? texto)
        {
            if (texto == null) return 1;
            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor))
            {
                return valor;
            }
            throw new DomainException($"Quantidade de volumes inválida: \"{texto}\".");
        }

        private static ModalidadePedido? Modalidade(string? texto)
        {
            if (texto == null) return null;
            string n = CamposImportacao.Normalizar(texto);

            if (n.Contains("retir") || n == "loja" || n.Contains("balcao")) return ModalidadePedido.Retirada;
            if (n.Contains("agend")) return ModalidadePedido.Agendada;
            if (n.Contains("mesmodia") || n == "hoje" || n == "expressa" || n == "normal") return ModalidadePedido.MesmoDia;

            throw new DomainException($"Modalidade \"{texto}\" não reconhecida. Use mesmo dia, agendada ou retirada.");
        }

        private (DateTimeOffset? Inicio, DateTimeOffset? Fim) Janela(string? data, string? inicio, string? fim, ModalidadePedido modalidade)
        {
            DateOnly? dia = null;
            TimeOnly? horaInicio = null, horaFim = null;

            // A coluna "data" pode trazer data e hora juntas
            if (data != null)
            {
                if (DateTime.TryParseExact(data, FormatosDataHora, PtBr, DateTimeStyles.None, out DateTime dataHora))
                {
                    dia = DateOnly.FromDateTime(dataHora);
                    horaInicio = TimeOnly.FromDateTime(dataHora);
                }
                else if (DateTime.TryParseExact(data, FormatosData, PtBr, DateTimeStyles.None, out DateTime soData))
                {
                    dia = DateOnly.FromDateTime(soData);
                }
                else
                {
                    throw new DomainException($"Data inválida: \"{data}\". Use dd/mm/aaaa.");
                }
            }

            if (inicio != null) horaInicio = Hora(inicio, ref dia);
            if (fim != null) horaFim = Hora(fim, ref dia);

            if (dia == null)
            {
                if (modalidade == ModalidadePedido.Agendada)
                {
                    throw new DomainException("Entrega agendada precisa da data.");
                }
                dia = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _fuso)); // mesmo dia = hoje
            }

            // Sem horário: o dia inteiro (08:00–18:00); só um horário: janela de 2 horas
            TimeOnly i = horaInicio ?? (horaFim.HasValue ? horaFim.Value.AddHours(-2) : new TimeOnly(8, 0));
            TimeOnly f = horaFim ?? (horaInicio.HasValue ? horaInicio.Value.AddHours(2) : new TimeOnly(18, 0));

            return (ParaUtc(dia.Value, i), ParaUtc(dia.Value, f));
        }

        private static TimeOnly Hora(string texto, ref DateOnly? dia)
        {
            if (TimeOnly.TryParseExact(texto.ToLowerInvariant(), FormatosHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly hora))
            {
                return hora;
            }

            // Excel às vezes manda "dd/MM/yyyy HH:mm" na coluna de hora
            if (DateTime.TryParseExact(texto, FormatosDataHora, PtBr, DateTimeStyles.None, out DateTime dataHora))
            {
                dia ??= DateOnly.FromDateTime(dataHora);
                return TimeOnly.FromDateTime(dataHora);
            }

            throw new DomainException($"Horário inválido: \"{texto}\". Use hh:mm.");
        }

        private DateTimeOffset ParaUtc(DateOnly dia, TimeOnly hora)
        {
            DateTime local = dia.ToDateTime(hora, DateTimeKind.Unspecified);
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, _fuso), TimeSpan.Zero);
        }
    }
}
