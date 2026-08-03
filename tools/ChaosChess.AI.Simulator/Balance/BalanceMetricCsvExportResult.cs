using System;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceMetricCsvExportResult
    {
        public BalanceMetricCsvExportResult(
            string decisionCsv,
            string componentCsv)
        {
            DecisionCsv = decisionCsv ?? throw new ArgumentNullException(nameof(decisionCsv));
            ComponentCsv = componentCsv ?? throw new ArgumentNullException(nameof(componentCsv));
        }

        public string DecisionCsv { get; }

        public string ComponentCsv { get; }
    }
}
