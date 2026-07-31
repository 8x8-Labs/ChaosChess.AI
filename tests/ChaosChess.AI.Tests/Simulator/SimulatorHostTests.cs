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
            Assert.Contains("Wrote 2 game row", stderr.ToString());
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
