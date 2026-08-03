using ChaosChess.AI.Simulator;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class SimulatorCliParserTests
    {
        [Fact]
        public void Parse_ValidFakeOptions_ReturnsOptions()
        {
            SimulatorCliParseResult result = SimulatorCliParser.Parse(new[]
            {
                "--games", "2",
                "--seed", "12345",
                "--max-ply", "1",
                "--multipv", "1",
                "--output", "out.csv",
                "--overwrite"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Options);
            Assert.Equal(2, result.Options.Games);
            Assert.Equal(12345, result.Options.Seed);
            Assert.Equal(1, result.Options.MaxPly);
            Assert.Equal(1, result.Options.MultiPv);
            Assert.Equal("out.csv", result.Options.OutputPath);
            Assert.True(result.Options.Overwrite);
        }

        [Fact]
        public void Parse_BalanceScenarioOption_ReturnsScenarioPath()
        {
            SimulatorCliParseResult result = SimulatorCliParser.Parse(new[]
            {
                "--output", "out.csv",
                "--balance-scenario", "scenario.json",
                "--balance-metrics-output", "metrics"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Options);
            Assert.Equal("scenario.json", result.Options.BalanceScenarioPath);
            Assert.Equal("metrics", result.Options.BalanceMetricsOutputPath);
        }

        [Fact]
        public void Parse_EngineOptions_AreStoredButNotRequiredForFakeMode()
        {
            SimulatorCliParseResult result = SimulatorCliParser.Parse(new[]
            {
                "--output", "out.csv",
                "--engine", "engine.exe",
                "--variant-config", "variants.ini",
                "--depth", "8"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Options);
            Assert.True(result.Options.IsEngineMode);
            Assert.Equal("engine.exe", result.Options.EnginePath);
            Assert.Equal("variants.ini", result.Options.VariantConfigPath);
            Assert.Equal(8, result.Options.Depth);
        }

        [Fact]
        public void Parse_Help_ReturnsHelpOptions()
        {
            SimulatorCliParseResult result = SimulatorCliParser.Parse(new[] { "--help" });

            Assert.True(result.Success);
            Assert.NotNull(result.Options);
            Assert.True(result.Options.ShowHelp);
        }

        [Theory]
        [InlineData("--unknown")]
        [InlineData("--games")]
        [InlineData("--games", "0")]
        [InlineData("--seed", "abc")]
        [InlineData("positional")]
        [InlineData("--output", "a.csv", "--output", "b.csv")]
        public void Parse_InvalidArgs_ReturnsError(params string[] args)
        {
            SimulatorCliParseResult result = SimulatorCliParser.Parse(args);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
