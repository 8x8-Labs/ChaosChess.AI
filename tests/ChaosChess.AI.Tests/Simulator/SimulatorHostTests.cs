using System;
using System.Collections.Generic;
using System.IO;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulator;
using ChaosChess.AI.Stockfish;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class SimulatorHostTests
    {
        [Fact]
        public void Run_FakeMode_WritesCsv()
        {
            using TempFile temp = TempFile.Create();
            var stderr = new StringWriter();

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--games", "2",
                "--seed", "12345",
                "--max-ply", "1",
                "--multipv", "1",
                "--output", temp.Path,
                "--overwrite"
            }, new StringWriter(), stderr);

            Assert.Equal(0, exitCode);
            string csv = File.ReadAllText(temp.Path);
            Assert.Contains("schema_version,batch_id", csv);
            Assert.Equal(3, csv.Split('\n').Length);
            AssertFakeRowsHaveNoCardRecommendations(csv);
            Assert.Contains("Wrote 2 game row", stderr.ToString());
        }

        [Fact]
        public void Run_BalanceScenario_WritesCsvWithBaselineCardRecommendation()
        {
            using TempFile output = TempFile.Create();
            using TempFile scenario = TempFile.Create();
            using TempDirectory metrics = TempDirectory.Create();
            File.WriteAllText(scenario.Path, @"
{
  ""scenarioId"": ""charge-strong"",
  ""schemaVersion"": 1,
  ""startingFen"": ""4k3/8/8/8/8/8/4P3/4K3 w - - 0 1"",
  ""actor"": ""White"",
  ""cards"": [
    { ""cardId"": ""charge"", ""category"": ""Mobility"", ""remainingUses"": 1 }
  ],
  ""scenarioGroup"": ""strong"",
  ""expectedBehavior"": ""ShouldUse""
}");

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--games", "1",
                "--seed", "12345",
                "--max-ply", "1",
                "--multipv", "1",
                "--output", output.Path,
                "--overwrite",
                "--balance-scenario", scenario.Path,
                "--balance-metrics-output", metrics.Path
            }, new StringWriter(), new StringWriter());

            Assert.Equal(0, exitCode);
            string csv = File.ReadAllText(output.Path);
            string[] columns = csv.TrimEnd().Split('\n')[1].TrimEnd('\r').Split(',');
            Assert.Equal("balance-charge-strong", columns[1]);
            Assert.Equal("charge-strong", columns[6]);
            Assert.Equal("1", columns[22]);
            Assert.Equal("0", columns[23]);
            Assert.Equal("not_applied_contract_missing", columns[24]);

            string decisionMetrics = File.ReadAllText(System.IO.Path.Combine(metrics.Path, "decision_metrics.csv"));
            string componentMetrics = File.ReadAllText(System.IO.Path.Combine(metrics.Path, "component_metrics.csv"));
            Assert.Contains("event_id,ply_index,actor,card_id", decisionMetrics);
            Assert.Contains("ply-0:card-0:charge,0,White,charge", decisionMetrics);
            Assert.Contains("event_id,card_id,candidate_rank,component_code", componentMetrics);
            Assert.Contains("charge.movable_pawns", componentMetrics);
        }

        [Fact]
        public void Run_BalanceMetricsOutputWithoutScenario_ReturnsExitCode2()
        {
            using TempFile output = TempFile.Create();
            using TempDirectory metrics = TempDirectory.Create();

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--games", "1",
                "--output", output.Path,
                "--overwrite",
                "--balance-metrics-output", metrics.Path
            }, new StringWriter(), new StringWriter());

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public void Run_ExistingOutputWithoutOverwrite_ReturnsExitCode3()
        {
            using TempFile temp = TempFile.Create();
            File.WriteAllText(temp.Path, "existing");

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--games", "1",
                "--output", temp.Path
            }, new StringWriter(), new StringWriter());

            Assert.Equal(3, exitCode);
            Assert.Equal("existing", File.ReadAllText(temp.Path));
        }

        [Fact]
        public void Run_Overwrite_ReplacesExistingOutput()
        {
            using TempFile temp = TempFile.Create();
            File.WriteAllText(temp.Path, "existing");

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--games", "1",
                "--max-ply", "1",
                "--output", temp.Path,
                "--overwrite"
            }, new StringWriter(), new StringWriter());

            Assert.Equal(0, exitCode);
            Assert.NotEqual("existing", File.ReadAllText(temp.Path));
        }

        [Fact]
        public void Run_InvalidArgs_ReturnsExitCode2()
        {
            int exitCode = new SimulatorHost().Run(new[] { "--games", "0" }, new StringWriter(), new StringWriter());

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public void Run_MissingOutput_ReturnsExitCode2()
        {
            int exitCode = new SimulatorHost().Run(new[] { "--games", "1" }, new StringWriter(), new StringWriter());

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public void Run_EngineMode_WritesCsvWithEngineProvenance()
        {
            using TempFile temp = TempFile.Create();
            using TempFile engine = TempFile.Create();
            using TempFile variant = TempFile.Create();
            File.WriteAllText(engine.Path, "engine");
            File.WriteAllText(variant.Path, "variant");

            int exitCode = new SimulatorHost(_ => new FakeEngine()).Run(new[]
            {
                "--output", temp.Path,
                "--overwrite",
                "--engine", engine.Path,
                "--variant-config", variant.Path,
                "--depth", "8",
                "--max-ply", "1",
                "--multipv", "1"
            }, new StringWriter(), new StringWriter());

            Assert.Equal(0, exitCode);
            string csv = File.ReadAllText(temp.Path);
            Assert.Contains(",ed9f6f25068608efd412958da4dfc19328ca3511251fa6d5f9c42baf230e32f8,cb7bf562420b82e97ee182f019c22e010e50b29334c036a8c23b8d7e617ed23c,8,1,1,", csv);
        }

        [Fact]
        public void Run_MissingEngineFile_ReturnsExitCode2()
        {
            using TempFile temp = TempFile.Create();
            using TempFile variant = TempFile.Create();
            File.WriteAllText(variant.Path, "variant");

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--output", temp.Path,
                "--overwrite",
                "--engine", temp.Path + ".missing",
                "--variant-config", variant.Path
            }, new StringWriter(), new StringWriter());

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public void Run_EngineFailure_ReturnsExitCode4()
        {
            using TempFile temp = TempFile.Create();
            using TempFile engine = TempFile.Create();
            using TempFile variant = TempFile.Create();
            File.WriteAllText(engine.Path, "engine");
            File.WriteAllText(variant.Path, "variant");

            int exitCode = new SimulatorHost(_ => new FailingEngine()).Run(new[]
            {
                "--output", temp.Path,
                "--overwrite",
                "--engine", engine.Path,
                "--variant-config", variant.Path
            }, new StringWriter(), new StringWriter());

            Assert.Equal(4, exitCode);
        }

        [Fact]
        public void Run_OnlyOneEnginePathOption_ReturnsExitCode2()
        {
            using TempFile temp = TempFile.Create();

            int exitCode = new SimulatorHost().Run(new[]
            {
                "--output", temp.Path,
                "--overwrite",
                "--engine", "engine.exe"
            }, new StringWriter(), new StringWriter());

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public void Run_SameSeed_WritesDeterministicCsv()
        {
            using TempFile first = TempFile.Create();
            using TempFile second = TempFile.Create();
            string[] firstArgs =
            {
                "--games", "2",
                "--seed", "12345",
                "--max-ply", "1",
                "--multipv", "1",
                "--output", first.Path,
                "--overwrite"
            };
            string[] secondArgs =
            {
                "--games", "2",
                "--seed", "12345",
                "--max-ply", "1",
                "--multipv", "1",
                "--output", second.Path,
                "--overwrite"
            };

            Assert.Equal(0, new SimulatorHost().Run(firstArgs, new StringWriter(), new StringWriter()));
            Assert.Equal(0, new SimulatorHost().Run(secondArgs, new StringWriter(), new StringWriter()));

            Assert.Equal(File.ReadAllText(first.Path), File.ReadAllText(second.Path));
        }

        [Fact]
        public void Run_Help_WritesUsageToStdout()
        {
            var stdout = new StringWriter();

            int exitCode = new SimulatorHost().Run(new[] { "--help" }, stdout, new StringWriter());

            Assert.Equal(0, exitCode);
            Assert.Contains("Usage:", stdout.ToString());
        }

        private sealed class TempFile : IDisposable
        {
            private TempFile(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public static TempFile Create()
            {
                string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChaosChess.AI.Tests");
                Directory.CreateDirectory(directory);
                return new TempFile(System.IO.Path.Combine(directory, Guid.NewGuid().ToString("N") + ".csv"));
            }

            public void Dispose()
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
        }

        private sealed class TempDirectory : IDisposable
        {
            private TempDirectory(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public static TempDirectory Create()
            {
                string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChaosChess.AI.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                return new TempDirectory(directory);
            }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
        }

        private static void AssertFakeRowsHaveNoCardRecommendations(string csv)
        {
            string[] rows = csv.TrimEnd().Split('\n');

            for (int rowIndex = 1; rowIndex < rows.Length; rowIndex++)
            {
                string[] columns = rows[rowIndex].TrimEnd('\r').Split(',');

                Assert.Equal("0", columns[22]);
                Assert.Equal("0", columns[23]);
                Assert.Equal(string.Empty, columns[24]);
            }
        }

        private sealed class FakeEngine : IChessEngine
        {
            public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
            {
                return new[] { new MoveCandidate("e2e4", 0, mateIn: null) };
            }

            public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
            {
                return new PositionEvaluation(boardState.SideToMove, 0, mateIn: null);
            }

            public bool IsInCheck(BoardState boardState)
            {
                return false;
            }
        }

        private sealed class FailingEngine : IChessEngine
        {
            public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
            {
                throw new StockfishEngineException(StockfishEngineErrorCode.Timeout, "timeout");
            }

            public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
            {
                throw new StockfishEngineException(StockfishEngineErrorCode.Timeout, "timeout");
            }

            public bool IsInCheck(BoardState boardState)
            {
                return false;
            }
        }
    }
}
