using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChaosChess.AI.Simulator.Csv
{
    public static class CsvWriter
    {
        public static string Write(IEnumerable<IReadOnlyList<string?>> rows)
        {
            var builder = new StringBuilder();
            bool firstRow = true;

            foreach (IReadOnlyList<string?> row in rows)
            {
                if (!firstRow)
                {
                    builder.Append('\n');
                }

                firstRow = false;

                for (int i = 0; i < row.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(Escape(row[i] ?? string.Empty));
                }
            }

            return builder.ToString();
        }

        public static string FormatInvariant(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;

            if (!quote)
            {
                return value;
            }

            var builder = new StringBuilder();
            builder.Append('"');

            foreach (char character in value)
            {
                if (character == '"')
                {
                    builder.Append("\"\"");
                }
                else
                {
                    builder.Append(character);
                }
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
