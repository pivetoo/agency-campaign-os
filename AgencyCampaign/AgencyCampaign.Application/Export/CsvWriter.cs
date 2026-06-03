using System.Text;

namespace AgencyCampaign.Application.Export
{
    // Serializador CSV proprio (sem dependencia externa) para exportacao dos relatorios financeiros.
    // Decisoes: separador ';' e decimal com virgula (cultura pt-BR) para abrir direto no Excel pt-BR;
    // guarda contra CSV injection (prefixa ' em celulas de texto que comecam com =,+,@,TAB,CR); o '-'
    // NAO e guardado para nao corromper valores monetarios negativos. Datas e numeros sao formatados
    // pelo chamador (FinancialReportExportService); aqui so tratamos escaping e injection.
    public static class CsvWriter
    {
        public const char Delimiter = ';';

        public static string Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
        {
            StringBuilder builder = new();
            AppendRow(builder, header);

            foreach (IReadOnlyList<string> row in rows)
            {
                AppendRow(builder, row);
            }

            return builder.ToString();
        }

        private static void AppendRow(StringBuilder builder, IReadOnlyList<string> cells)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(Delimiter);
                }

                builder.Append(EscapeCell(cells[index]));
            }

            builder.Append("\r\n");
        }

        private static string EscapeCell(string? value)
        {
            string cell = value ?? string.Empty;

            if (cell.Length > 0 && (cell[0] == '=' || cell[0] == '+' || cell[0] == '@' || cell[0] == '\t' || cell[0] == '\r'))
            {
                cell = "'" + cell;
            }

            bool needsQuote = cell.Contains(Delimiter) || cell.Contains('"') || cell.Contains('\n') || cell.Contains('\r');
            if (needsQuote)
            {
                cell = "\"" + cell.Replace("\"", "\"\"") + "\"";
            }

            return cell;
        }
    }
}
