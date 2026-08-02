using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardTargetStrategyContext
    {
        private readonly ReadOnlyCollection<MoveCandidate> _engineTopMoves;

        public CardTargetStrategyContext(
            GameState gameState,
            CardInfo card,
            PieceColor actor,
            CardTargetingOptions? options = null,
            IEnumerable<MoveCandidate>? engineTopMoves = null)
        {
            EnsureValidColor(actor);

            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Actor = actor;
            Options = options ?? new CardTargetingOptions();
            _engineTopMoves = CopyEngineTopMoves(engineTopMoves);
        }

        public GameState GameState { get; }

        public CardInfo Card { get; }

        public PieceColor Actor { get; }

        public CardTargetingOptions Options { get; }

        public IReadOnlyList<MoveCandidate> EngineTopMoves => _engineTopMoves;

        private static ReadOnlyCollection<MoveCandidate> CopyEngineTopMoves(
            IEnumerable<MoveCandidate>? moves)
        {
            var copy = new List<MoveCandidate>();

            if (moves == null)
            {
                return copy.AsReadOnly();
            }

            foreach (MoveCandidate move in moves)
            {
                if (move == null)
                {
                    throw new ArgumentException(
                        "Engine top move collection cannot contain null.",
                        nameof(moves));
                }

                copy.Add(move);
            }

            return copy.AsReadOnly();
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
