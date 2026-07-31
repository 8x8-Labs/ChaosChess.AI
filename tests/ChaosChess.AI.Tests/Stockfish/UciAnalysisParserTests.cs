using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Stockfish;
using Xunit;

namespace ChaosChess.AI.Tests.Stockfish
{
    public sealed class UciAnalysisParserTests
    {
        [Fact]
        public void TryParseInfoLine_CentipawnScore_ReturnsAnalysisInfo()
        {
            bool parsed = UciAnalysisParser.TryParseInfoLine(
                "info depth 8 seldepth 10 multipv 2 score cp -34 nodes 120 pv e2e4 e7e5",
                out UciAnalysisInfo? info);

            Assert.True(parsed);
            Assert.NotNull(info);
            Assert.Equal(8, info.Depth);
            Assert.Equal(2, info.Multipv);
            Assert.Equal(-34, info.ScoreCentipawns);
            Assert.Null(info.MateIn);
            Assert.Equal(UciScoreBound.Exact, info.Bound);
            Assert.Equal(new[] { "e2e4", "e7e5" }, info.PrincipalVariation);
        }

        [Fact]
        public void TryParseInfoLine_MateScore_ReturnsAnalysisInfo()
        {
            bool parsed = UciAnalysisParser.TryParseInfoLine(
                "info depth 12 multipv 1 score mate -3 pv h2h1q",
                out UciAnalysisInfo? info);

            Assert.True(parsed);
            Assert.NotNull(info);
            Assert.Null(info.ScoreCentipawns);
            Assert.Equal(-3, info.MateIn);
        }

        [Theory]
        [InlineData("lowerbound", UciScoreBound.Lower)]
        [InlineData("upperbound", UciScoreBound.Upper)]
        public void TryParseInfoLine_BoundScore_ReturnsBound(string token, UciScoreBound expected)
        {
            bool parsed = UciAnalysisParser.TryParseInfoLine(
                "info depth 9 score cp 42 " + token + " pv a2a4",
                out UciAnalysisInfo? info);

            Assert.True(parsed);
            Assert.NotNull(info);
            Assert.Equal(expected, info.Bound);
        }

        [Fact]
        public void TryParseInfoLine_WithoutMultipv_DefaultsToFirstVariation()
        {
            bool parsed = UciAnalysisParser.TryParseInfoLine(
                "info depth 7 score cp 12 pv g1f3",
                out UciAnalysisInfo? info);

            Assert.True(parsed);
            Assert.NotNull(info);
            Assert.Equal(1, info.Multipv);
        }

        [Fact]
        public void TryParseInfoLine_UnknownLine_IsIgnored()
        {
            bool parsed = UciAnalysisParser.TryParseInfoLine(
                "info string NNUE evaluation unavailable",
                out UciAnalysisInfo? info);

            Assert.False(parsed);
            Assert.Null(info);
        }

        [Fact]
        public void TryParseBestMoveLine_MoveAndPonder_ReturnsBestMove()
        {
            bool parsed = UciAnalysisParser.TryParseBestMoveLine(
                "bestmove e2e4 ponder e7e5",
                out UciBestMove? bestMove);

            Assert.True(parsed);
            Assert.NotNull(bestMove);
            Assert.False(bestMove.IsNone);
            Assert.Equal("e2e4", bestMove.Move);
            Assert.Equal("e7e5", bestMove.PonderMove);
        }

        [Fact]
        public void TryParseBestMoveLine_None_ReturnsNoMove()
        {
            bool parsed = UciAnalysisParser.TryParseBestMoveLine(
                "bestmove none",
                out UciBestMove? bestMove);

            Assert.True(parsed);
            Assert.NotNull(bestMove);
            Assert.True(bestMove.IsNone);
            Assert.Null(bestMove.Move);
            Assert.Null(bestMove.PonderMove);
        }

        [Fact]
        public void ToMoveCandidates_UsesLatestDepthPerMultipv()
        {
            var infos = new List<UciAnalysisInfo>
            {
                Info(depth: 6, multipv: 1, cp: 10, move: "e2e4"),
                Info(depth: 8, multipv: 1, cp: 18, move: "d2d4"),
                Info(depth: 7, multipv: 2, cp: 4, move: "g1f3")
            };

            IReadOnlyList<MoveCandidate> candidates = UciAnalysisParser.ToMoveCandidates(infos, variationCount: 2);

            Assert.Equal(2, candidates.Count);
            Assert.Equal("d2d4", candidates[0].UciMove);
            Assert.Equal(18, candidates[0].ScoreCentipawns);
            Assert.Equal("g1f3", candidates[1].UciMove);
        }

        [Fact]
        public void ToMoveCandidates_ReturnsFewerThanRequestedWhenEngineHasFewer()
        {
            var infos = new List<UciAnalysisInfo>
            {
                Info(depth: 8, multipv: 1, cp: 10, move: "e2e4")
            };

            IReadOnlyList<MoveCandidate> candidates = UciAnalysisParser.ToMoveCandidates(infos, variationCount: 3);

            Assert.Single(candidates);
        }

        [Fact]
        public void ToMoveCandidates_SkipsInvalidAndDuplicatePvMoves()
        {
            var infos = new List<UciAnalysisInfo>
            {
                Info(depth: 8, multipv: 1, cp: 10, move: "e2e4"),
                Info(depth: 8, multipv: 2, cp: 8, move: "e2e4"),
                Info(depth: 8, multipv: 3, cp: 6, move: "invalid"),
                Info(depth: 8, multipv: 4, cp: 4, move: "g1f3")
            };

            IReadOnlyList<MoveCandidate> candidates = UciAnalysisParser.ToMoveCandidates(infos, variationCount: 4);

            Assert.Equal(2, candidates.Count);
            Assert.Equal("e2e4", candidates[0].UciMove);
            Assert.Equal("g1f3", candidates[1].UciMove);
        }

        [Fact]
        public void ToMoveCandidates_MateScore_FillsOnlyMateDistance()
        {
            var infos = new List<UciAnalysisInfo>
            {
                new UciAnalysisInfo(
                    depth: 8,
                    multipv: 1,
                    scoreCentipawns: null,
                    mateIn: 2,
                    UciScoreBound.Exact,
                    new[] { "h5f7" })
            };

            IReadOnlyList<MoveCandidate> candidates = UciAnalysisParser.ToMoveCandidates(infos, variationCount: 1);

            MoveCandidate candidate = Assert.Single(candidates);
            Assert.Null(candidate.ScoreCentipawns);
            Assert.Equal(2, candidate.MateIn);
        }

        private static UciAnalysisInfo Info(int depth, int multipv, int cp, string move)
        {
            return new UciAnalysisInfo(
                depth,
                multipv,
                scoreCentipawns: cp,
                mateIn: null,
                UciScoreBound.Exact,
                new[] { move });
        }
    }
}
