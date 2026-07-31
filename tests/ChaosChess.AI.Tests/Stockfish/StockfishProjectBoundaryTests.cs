using System;
using System.IO;
using Xunit;

namespace ChaosChess.AI.Tests.Stockfish
{
    public sealed class StockfishProjectBoundaryTests
    {
        [Fact]
        public void CoreProject_DoesNotReferenceAdapterOnlyApis()
        {
            string coreRoot = Path.Combine(FindRepositoryRoot(), "src", "ChaosChess.AI");
            string[] forbiddenTerms =
            {
                "System.Diagnostics." + "Process",
                "System." + "IO",
                "Unity" + "Engine",
                "DO" + "Tween",
                "Fairy" + "StockfishBridge"
            };

            foreach (string path in Directory.EnumerateFiles(coreRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);

                foreach (string term in forbiddenTerms)
                {
                    Assert.DoesNotContain(term, source, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void SolutionSources_DoNotReferenceUnityAdapterTypes()
        {
            string repositoryRoot = FindRepositoryRoot();
            string[] sourceRoots =
            {
                Path.Combine(repositoryRoot, "src"),
                Path.Combine(repositoryRoot, "tests"),
                Path.Combine(repositoryRoot, "tools")
            };
            string[] forbiddenTerms =
            {
                "Unity" + "Engine",
                "DO" + "Tween",
                "Fairy" + "StockfishBridge"
            };

            foreach (string sourceRoot in sourceRoots)
            {
                if (!Directory.Exists(sourceRoot))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
                {
                    string source = File.ReadAllText(path);

                    foreach (string term in forbiddenTerms)
                    {
                        Assert.DoesNotContain(term, source, StringComparison.Ordinal);
                    }
                }
            }
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ChaosChess.AI.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate ChaosChess.AI.sln.");
        }
    }
}
