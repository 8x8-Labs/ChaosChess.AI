using System;
using System.Collections.Generic;

namespace ChaosChess.AI.Simulator
{
    public static class SimulatorCliParser
    {
        public static SimulatorCliParseResult Parse(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool showHelp = false;
            int games = 1;
            int seed = 0;
            int maxPly = HeadlessGameOptions.DefaultMaxPly;
            int multipv = HeadlessGameOptions.DefaultVariationCount;
            string? outputPath = null;
            bool overwrite = false;
            string? enginePath = null;
            string? variantConfigPath = null;
            int depth = 8;
            string? balanceScenarioPath = null;
            string? balanceMetricsOutputPath = null;
            var seenOptions = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < args.Length; i++)
            {
                string option = args[i];

                if (option == "--help" || option == "-h")
                {
                    showHelp = true;
                    continue;
                }

                if (option == "--overwrite")
                {
                    overwrite = true;
                    continue;
                }

                if (!option.StartsWith("--", StringComparison.Ordinal))
                {
                    return SimulatorCliParseResult.Error("Unexpected positional argument: " + option);
                }

                if (!seenOptions.Add(option))
                {
                    return SimulatorCliParseResult.Error("Duplicate option: " + option);
                }

                if (i + 1 >= args.Length)
                {
                    return SimulatorCliParseResult.Error("Missing value for option: " + option);
                }

                string value = args[++i];

                switch (option)
                {
                    case "--games":
                        if (!TryParsePositiveInt(value, out games))
                        {
                            return SimulatorCliParseResult.Error("--games must be a positive integer.");
                        }

                        break;
                    case "--seed":
                        if (!int.TryParse(value, out seed))
                        {
                            return SimulatorCliParseResult.Error("--seed must be an integer.");
                        }

                        break;
                    case "--max-ply":
                        if (!TryParsePositiveInt(value, out maxPly))
                        {
                            return SimulatorCliParseResult.Error("--max-ply must be a positive integer.");
                        }

                        break;
                    case "--multipv":
                        if (!TryParsePositiveInt(value, out multipv))
                        {
                            return SimulatorCliParseResult.Error("--multipv must be a positive integer.");
                        }

                        break;
                    case "--output":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return SimulatorCliParseResult.Error("--output cannot be empty.");
                        }

                        outputPath = value;
                        break;
                    case "--engine":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return SimulatorCliParseResult.Error("--engine cannot be empty.");
                        }

                        enginePath = value;
                        break;
                    case "--variant-config":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return SimulatorCliParseResult.Error("--variant-config cannot be empty.");
                        }

                        variantConfigPath = value;
                        break;
                    case "--depth":
                        if (!TryParsePositiveInt(value, out depth))
                        {
                            return SimulatorCliParseResult.Error("--depth must be a positive integer.");
                        }

                        break;
                    case "--balance-scenario":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return SimulatorCliParseResult.Error("--balance-scenario cannot be empty.");
                        }

                        balanceScenarioPath = value;
                        break;
                    case "--balance-metrics-output":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return SimulatorCliParseResult.Error("--balance-metrics-output cannot be empty.");
                        }

                        balanceMetricsOutputPath = value;
                        break;
                    default:
                        return SimulatorCliParseResult.Error("Unknown option: " + option);
                }
            }

            return SimulatorCliParseResult.Ok(new SimulatorCliOptions(
                showHelp,
                games,
                seed,
                maxPly,
                multipv,
                outputPath,
                overwrite,
                enginePath,
                variantConfigPath,
                depth,
                balanceScenarioPath,
                balanceMetricsOutputPath));
        }

        private static bool TryParsePositiveInt(string value, out int result)
        {
            return int.TryParse(value, out result) && result > 0;
        }
    }
}
