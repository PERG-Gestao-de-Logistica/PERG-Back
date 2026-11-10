using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Perg.Api.Exceptions;

namespace Perg.Api.Applications.Importacoes
{
    public class LinhaPlanilha
    {
        public int Numero { get; set; }                   // número da linha na planilha (cabeçalho = 1)
        public string[] Valores { get; set; } = [];
    }

    public class PlanilhaLida
    {
        public List<string> Cabecalhos { get; set; } = new();
        public List<LinhaPlanilha> Linhas { get; set; } = new();
    }

    // Lê .xlsx (ClosedXML, primeira aba) e .csv (; , ou tab; UTF-8 ou Latin-1 do Excel).
    // Primeira linha não vazia = cabeçalho. Linhas totalmente vazias são ignoradas.
    public static class LeitorPlanilha
    {
        public const int MaximoLinhas = 2000;

        public static PlanilhaLida Ler(Stream conteudo, string extensao)
        {
            PlanilhaLida planilha = extensao == ".xlsx" ? LerXlsx(conteudo) : LerCsv(conteudo);

            if (planilha.Cabecalhos.Count == 0)
            {
                throw new DomainException("A planilha está vazia.");
            }

            if (planilha.Linhas.Count == 0)
            {
                throw new DomainException("A planilha só tem o cabeçalho, sem pedidos.");
            }

            if (planilha.Linhas.Count > MaximoLinhas)
            {
                throw new DomainException($"A planilha tem {planilha.Linhas.Count} linhas. O máximo por importação é {MaximoLinhas}.");
            }

            // Cabeçalhos repetidos ou vazios ganham um nome único (o mapeamento usa o nome)
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < planilha.Cabecalhos.Count; i++)
            {
                string nome = string.IsNullOrWhiteSpace(planilha.Cabecalhos[i]) ? $"Coluna {i + 1}" : planilha.Cabecalhos[i].Trim();
                string unico = nome;
                for (int n = 2; !usados.Add(unico); n++) unico = $"{nome} ({n})";
                planilha.Cabecalhos[i] = unico;
            }

            return planilha;
        }

        // ---------- XLSX ----------

        private static PlanilhaLida LerXlsx(Stream conteudo)
        {
            XLWorkbook pasta;
            try
            {
                pasta = new XLWorkbook(conteudo);
            }
            catch (Exception)
            {
                throw new DomainException("Não foi possível abrir o arquivo .xlsx. Salve de novo no Excel e tente outra vez.");
            }

            using (pasta)
            {
                IXLWorksheet aba = pasta.Worksheets.First();
                IXLRange? usado = aba.RangeUsed();
                var planilha = new PlanilhaLida();
                if (usado == null) return planilha;

                int primeiraColuna = usado.FirstColumn().ColumnNumber();
                int ultimaColuna = usado.LastColumn().ColumnNumber();
                int ultimaLinha = usado.LastRow().RowNumber();
                bool achouCabecalho = false;

                for (int l = usado.FirstRow().RowNumber(); l <= ultimaLinha; l++)
                {
                    string[] valores = new string[ultimaColuna - primeiraColuna + 1];
                    for (int c = primeiraColuna; c <= ultimaColuna; c++)
                    {
                        valores[c - primeiraColuna] = Texto(aba.Cell(l, c));
                    }

                    if (valores.All(string.IsNullOrWhiteSpace)) continue;

                    if (!achouCabecalho)
                    {
                        planilha.Cabecalhos = valores.ToList();
                        achouCabecalho = true;
                    }
                    else
                    {
                        planilha.Linhas.Add(new LinhaPlanilha { Numero = l, Valores = valores });
                        if (planilha.Linhas.Count > MaximoLinhas) break;
                    }
                }

                return planilha;
            }
        }

        private static string Texto(IXLCell celula)
        {
            if (celula.IsEmpty()) return string.Empty;

            switch (celula.DataType)
            {
                case XLDataType.DateTime:
                    DateTime data = celula.GetDateTime();
                    return data.TimeOfDay == TimeSpan.Zero ? data.ToString("dd/MM/yyyy") : data.ToString("dd/MM/yyyy HH:mm");
                case XLDataType.TimeSpan:
                    return celula.GetTimeSpan().ToString(@"hh\:mm");
                case XLDataType.Number:
                    // Sem notação científica (telefone/CEP guardados como número)
                    return celula.GetDouble().ToString("0.##########", CultureInfo.InvariantCulture);
                case XLDataType.Boolean:
                    return celula.GetBoolean() ? "sim" : "não";
                case XLDataType.Error:
                    return string.Empty;
                default:
                    return celula.GetString().Trim();
            }
        }

        // ---------- CSV ----------

        private static PlanilhaLida LerCsv(Stream conteudo)
        {
            using var memoria = new MemoryStream();
            conteudo.CopyTo(memoria);
            string texto = Decodificar(memoria.ToArray());

            List<List<string>> registros = SepararRegistros(texto, DetectarSeparador(texto));
            var planilha = new PlanilhaLida();
            bool achouCabecalho = false;

            for (int i = 0; i < registros.Count; i++)
            {
                List<string> valores = registros[i];
                if (valores.All(string.IsNullOrWhiteSpace)) continue;

                if (!achouCabecalho)
                {
                    planilha.Cabecalhos = valores.Select(v => v.Trim()).ToList();
                    achouCabecalho = true;
                    continue;
                }

                // Mesmo número de colunas do cabeçalho (completa ou corta)
                string[] ajustados = new string[planilha.Cabecalhos.Count];
                for (int c = 0; c < ajustados.Length; c++)
                {
                    ajustados[c] = c < valores.Count ? valores[c].Trim() : string.Empty;
                }

                planilha.Linhas.Add(new LinhaPlanilha { Numero = i + 1, Valores = ajustados });
                if (planilha.Linhas.Count > MaximoLinhas) break;
            }

            return planilha;
        }

        // O Excel brasileiro salva CSV em Windows-1252; o resto costuma ser UTF-8
        private static string Decodificar(byte[] bytes)
        {
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Latin1.GetString(bytes);
            }
        }

        private static char DetectarSeparador(string texto)
        {
            int fim = texto.IndexOf('\n');
            string primeiraLinha = fim < 0 ? texto : texto[..fim];

            char[] candidatos = [';', ',', '\t'];
            return candidatos.OrderByDescending(c => primeiraLinha.Count(x => x == c)).First();
        }

        // CSV com aspas: "Rua A, 10" e "" dentro de aspas
        private static List<List<string>> SepararRegistros(string texto, char separador)
        {
            var registros = new List<List<string>>();
            var atual = new List<string>();
            var campo = new StringBuilder();
            bool dentroAspas = false;

            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];

                if (dentroAspas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < texto.Length && texto[i + 1] == '"') { campo.Append('"'); i++; }
                        else dentroAspas = false;
                    }
                    else campo.Append(c);
                }
                else if (c == '"') dentroAspas = true;
                else if (c == separador) { atual.Add(campo.ToString()); campo.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                    atual.Add(campo.ToString()); campo.Clear();
                    registros.Add(atual); atual = new List<string>();
                }
                else campo.Append(c);
            }

            if (campo.Length > 0 || atual.Count > 0)
            {
                atual.Add(campo.ToString());
                registros.Add(atual);
            }

            return registros;
        }
    }
}
