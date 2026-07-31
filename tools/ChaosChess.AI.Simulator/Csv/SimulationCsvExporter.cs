using System;
using System.Collections.Generic;

namespace ChaosChess.AI.Simulator.Csv
{
    public static class SimulationCsvExporter
    {
        public static string Export(BatchSimulationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var rows = new List<IReadOnlyList<string?>>();
            rows.Add(SimulationCsvSchema.Header);

            foreach (BatchGameResult game in result.Games)
            {
                rows.Add(SimulationCsvSchema.ToRow(result.Options, game));
            }

            return CsvWriter.Write(rows);
        }
    }
}
