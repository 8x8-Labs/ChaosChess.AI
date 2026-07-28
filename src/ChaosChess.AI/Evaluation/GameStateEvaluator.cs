using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Evaluation
{
    public sealed class GameStateEvaluator
    {
        private const double ScoreNormalizationDivisor = 13.0;
        private const int PredictedMateScore = 90;
        private const int MaximumNonMateBoardScore = PredictedMateScore - 1;
        private const int MaximumNonTerminalTotalScore = 99;

        private static readonly IReadOnlyDictionary<string, TileThreatRule> ThreatRules =
            new Dictionary<string, TileThreatRule>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mine"] = new TileThreatRule(radius: 1, weight: 1.0),
                ["Fire"] = new TileThreatRule(radius: 1, weight: 0.8)
            };

        private static readonly IReadOnlyDictionary<string, int> AdvantageScores =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Blessing"] = 30,
                ["Peace"] = 20,
                ["Portal"] = 15
            };

        private readonly IChessEngine _chessEngine;
        private readonly EvaluationOptions _options;

        public GameStateEvaluator(
            IChessEngine chessEngine,
            EvaluationOptions? options = null)
        {
            _chessEngine = chessEngine ?? throw new ArgumentNullException(nameof(chessEngine));
            _options = options ?? new EvaluationOptions();
        }

        public EvaluationResult Evaluate(GameState gameState, PieceColor perspective)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            EnsureValidColor(perspective);

            PositionEvaluation positionEvaluation = _chessEngine.EvaluatePosition(
                gameState.BoardState,
                _options.SearchDepth);

            if (positionEvaluation == null)
            {
                throw new InvalidOperationException("Chess engine returned no position evaluation.");
            }

            EngineScore engineScore = NormalizeEngineScore(positionEvaluation, perspective);
            int threat = EvaluateThreat(gameState, perspective);
            int advantage = EvaluateAdvantage(gameState, perspective);
            int totalScore = ClampAndRound(
                (engineScore.BoardScore * _options.BoardScoreWeight) +
                (threat * _options.ThreatWeight) +
                (advantage * _options.AdvantageWeight),
                -MaximumNonTerminalTotalScore,
                MaximumNonTerminalTotalScore);

            return new EvaluationResult(
                engineScore.BoardScore,
                engineScore.MateIn,
                threat,
                advantage,
                totalScore);
        }

        private static EngineScore NormalizeEngineScore(
            PositionEvaluation evaluation,
            PieceColor perspective)
        {
            bool samePerspective = evaluation.Perspective == perspective;

            if (evaluation.ScoreCentipawns.HasValue)
            {
                int centipawns = samePerspective
                    ? evaluation.ScoreCentipawns.Value
                    : -evaluation.ScoreCentipawns.Value;

                return new EngineScore(
                    NormalizeBoardCentipawnScore(centipawns),
                    mateIn: null);
            }

            int mateIn = evaluation.MateIn!.Value;
            int normalizedMateIn = samePerspective ? mateIn : -mateIn;
            int boardScore = normalizedMateIn > 0
                ? PredictedMateScore
                : -PredictedMateScore;

            return new EngineScore(boardScore, normalizedMateIn);
        }

        private static int EvaluateThreat(GameState gameState, PieceColor perspective)
        {
            double balance = 0;

            foreach (TileEffectInfo effect in gameState.TileEffects)
            {
                if (!effect.Owner.HasValue ||
                    !ThreatRules.TryGetValue(effect.EffectType, out TileThreatRule rule))
                {
                    continue;
                }

                PieceColor effectOwner = effect.Owner.Value;

                foreach (PieceInfo piece in gameState.BoardState.Pieces)
                {
                    if (piece.Color == effectOwner ||
                        ChebyshevDistance(piece.Square, effect.Square) > rule.Radius)
                    {
                        continue;
                    }

                    double threatenedValue = GetPieceValue(piece.Kind) * rule.Weight;
                    balance += effectOwner == perspective ? threatenedValue : -threatenedValue;
                }
            }

            return NormalizeCentipawnScore(balance);
        }

        private static int EvaluateAdvantage(GameState gameState, PieceColor perspective)
        {
            int balance = 0;

            foreach (TileEffectInfo effect in gameState.TileEffects)
            {
                if (!effect.Owner.HasValue ||
                    !AdvantageScores.TryGetValue(effect.EffectType, out int score))
                {
                    continue;
                }

                balance += effect.Owner.Value == perspective ? score : -score;
            }

            return Clamp(balance);
        }

        private static int GetPieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 100;
                case PieceKind.Knight:
                    return 320;
                case PieceKind.Bishop:
                    return 330;
                case PieceKind.Rook:
                    return 500;
                case PieceKind.Queen:
                    return 900;
                case PieceKind.Amazon:
                    return 1300;
                case PieceKind.Chancellor:
                    return 900;
                case PieceKind.KnightRider:
                    return 700;
                default:
                    return 0;
            }
        }

        private static int NormalizeCentipawnScore(int score)
        {
            return NormalizeCentipawnScore((double)score);
        }

        private static int NormalizeBoardCentipawnScore(int score)
        {
            int rounded = (int)Math.Round(
                score / ScoreNormalizationDivisor,
                MidpointRounding.AwayFromZero);

            return Clamp(
                rounded,
                -MaximumNonMateBoardScore,
                MaximumNonMateBoardScore);
        }

        private static int NormalizeCentipawnScore(double score)
        {
            return ClampAndRound(score / ScoreNormalizationDivisor);
        }

        private static int ChebyshevDistance(Square left, Square right)
        {
            return Math.Max(
                Math.Abs(left.File - right.File),
                Math.Abs(left.Rank - right.Rank));
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }

        private static int ClampAndRound(double value)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return Clamp(rounded);
        }

        private static int ClampAndRound(double value, int minimum, int maximum)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return Clamp(rounded, minimum, maximum);
        }

        private static int Clamp(int value)
        {
            return Clamp(value, -100, 100);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }

        private readonly struct EngineScore
        {
            public EngineScore(int boardScore, int? mateIn)
            {
                BoardScore = boardScore;
                MateIn = mateIn;
            }

            public int BoardScore { get; }

            public int? MateIn { get; }
        }

        private readonly struct TileThreatRule
        {
            public TileThreatRule(int radius, double weight)
            {
                Radius = radius;
                Weight = weight;
            }

            public int Radius { get; }

            public double Weight { get; }
        }
    }
}
