using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Evaluation
{
    public sealed class GameStateEvaluator
    {
        private const double ScoreNormalizationDivisor = 13.0;
        private const int DirectKingAttackRisk = 50;
        private const int KingRingAttackRisk = 6;

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

        private readonly EvaluationOptions _options;

        public GameStateEvaluator(EvaluationOptions? options = null)
        {
            _options = options ?? new EvaluationOptions();
        }

        public EvaluationResult Evaluate(GameState gameState, PieceColor perspective)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            EnsureValidColor(perspective);

            PieceColor opponent = OpponentOf(perspective);
            bool perspectiveKingExists = HasKing(gameState.BoardState, perspective);
            bool opponentKingExists = HasKing(gameState.BoardState, opponent);

            if (!perspectiveKingExists || !opponentKingExists)
            {
                return CreateTerminalResult(perspectiveKingExists, opponentKingExists);
            }

            int material = EvaluateMaterial(gameState.BoardState, perspective);
            int threat = EvaluateThreat(gameState, perspective);
            int advantage = EvaluateAdvantage(gameState, perspective);
            int kingSafety = EvaluateKingSafety(gameState.BoardState, perspective);
            int totalScore = ClampAndRound(
                (material * _options.MaterialWeight) +
                (threat * _options.ThreatWeight) +
                (advantage * _options.AdvantageWeight) +
                (kingSafety * _options.KingSafetyWeight));

            return new EvaluationResult(material, threat, advantage, kingSafety, totalScore);
        }

        private static int EvaluateMaterial(BoardState boardState, PieceColor perspective)
        {
            int balance = 0;

            foreach (PieceInfo piece in boardState.Pieces)
            {
                int value = GetPieceValue(piece.Kind);

                if (piece.Color == perspective)
                {
                    balance += value;
                }
                else
                {
                    balance -= value;
                }
            }

            return NormalizeCentipawnScore(balance);
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

        private static int EvaluateKingSafety(BoardState boardState, PieceColor perspective)
        {
            PieceColor opponent = OpponentOf(perspective);
            HashSet<Square> opponentAttacks = PieceAttackMap.Create(boardState, opponent);
            HashSet<Square> perspectiveAttacks = PieceAttackMap.Create(boardState, perspective);
            int perspectiveRisk = EvaluateKingRisk(boardState, perspective, opponentAttacks);
            int opponentRisk = EvaluateKingRisk(boardState, opponent, perspectiveAttacks);

            return Clamp(opponentRisk - perspectiveRisk);
        }

        private static int EvaluateKingRisk(
            BoardState boardState,
            PieceColor kingColor,
            HashSet<Square> opposingAttacks)
        {
            PieceInfo? king = FindKing(boardState, kingColor);

            if (king == null)
            {
                return 100;
            }

            int risk = opposingAttacks.Contains(king.Square) ? DirectKingAttackRisk : 0;

            for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
            {
                for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
                {
                    if ((fileOffset == 0 && rankOffset == 0) ||
                        !IsOnBoard(king.Square.File + fileOffset, king.Square.Rank + rankOffset))
                    {
                        continue;
                    }

                    var adjacentSquare = new Square(
                        king.Square.File + fileOffset,
                        king.Square.Rank + rankOffset);

                    if (opposingAttacks.Contains(adjacentSquare))
                    {
                        risk += KingRingAttackRisk;
                    }
                }
            }

            return Clamp(risk, 0, 100);
        }

        private static EvaluationResult CreateTerminalResult(
            bool perspectiveKingExists,
            bool opponentKingExists)
        {
            if (perspectiveKingExists == opponentKingExists)
            {
                return new EvaluationResult(0, 0, 0, 0, 0);
            }

            int score = perspectiveKingExists ? 100 : -100;
            return new EvaluationResult(0, 0, 0, score, score);
        }

        private static bool HasKing(BoardState boardState, PieceColor color)
        {
            return FindKing(boardState, color) != null;
        }

        private static PieceInfo? FindKing(BoardState boardState, PieceColor color)
        {
            foreach (PieceInfo piece in boardState.Pieces)
            {
                if (piece.Color == color && piece.Kind == PieceKind.King)
                {
                    return piece;
                }
            }

            return null;
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

        private static PieceColor OpponentOf(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }

        private static bool IsOnBoard(int file, int rank)
        {
            return file >= 0 &&
                   file < Square.BoardSize &&
                   rank >= 0 &&
                   rank < Square.BoardSize;
        }

        private static int ClampAndRound(double value)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return Clamp(rounded);
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
