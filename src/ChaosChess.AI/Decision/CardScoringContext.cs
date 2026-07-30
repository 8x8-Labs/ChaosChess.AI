using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;

namespace ChaosChess.AI.Decision
{
    public sealed class CardScoringContext
    {
        public CardScoringContext(
            GameState gameState,
            CardInfo card,
            PieceColor perspective,
            EvaluationResult currentEvaluation,
            int currentScore)
        {
            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            Card = card ?? throw new ArgumentNullException(nameof(card));
            EnsureValidColor(perspective);
            CurrentEvaluation = currentEvaluation ?? throw new ArgumentNullException(nameof(currentEvaluation));
            CurrentScore = currentScore;
            Perspective = perspective;
        }

        public GameState GameState { get; }

        public CardInfo Card { get; }

        public PieceColor Perspective { get; }

        public EvaluationResult CurrentEvaluation { get; }

        public int CurrentScore { get; }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
