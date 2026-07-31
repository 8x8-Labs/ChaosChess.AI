using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator
{
    public sealed class HeadlessGameResult
    {
        private readonly ReadOnlyCollection<string> _warnings;

        public HeadlessGameResult(
            GameState initialState,
            GameState finalState,
            int plyCount,
            GameResult result,
            PieceColor? winner,
            GameTerminationReason terminationReason,
            int cardsRecommended,
            int cardsApplied,
            string? cardsSkippedReason,
            IEnumerable<string> warnings,
            string? errorCode)
        {
            if (plyCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plyCount), plyCount, "Ply count cannot be negative.");
            }

            if (cardsRecommended < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardsRecommended), cardsRecommended, "Recommended card count cannot be negative.");
            }

            if (cardsApplied < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardsApplied), cardsApplied, "Applied card count cannot be negative.");
            }

            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
            PlyCount = plyCount;
            Result = result;
            Winner = winner;
            TerminationReason = terminationReason;
            CardsRecommended = cardsRecommended;
            CardsApplied = cardsApplied;
            CardsSkippedReason = cardsSkippedReason;
            _warnings = CopyWarnings(warnings);
            ErrorCode = errorCode;
        }

        public GameState InitialState { get; }

        public GameState FinalState { get; }

        public int PlyCount { get; }

        public GameResult Result { get; }

        public PieceColor? Winner { get; }

        public GameTerminationReason TerminationReason { get; }

        public int CardsRecommended { get; }

        public int CardsApplied { get; }

        public string? CardsSkippedReason { get; }

        public IReadOnlyList<string> Warnings => _warnings;

        public string? ErrorCode { get; }

        private static ReadOnlyCollection<string> CopyWarnings(IEnumerable<string> warnings)
        {
            if (warnings == null)
            {
                throw new ArgumentNullException(nameof(warnings));
            }

            var copy = new List<string>();

            foreach (string warning in warnings)
            {
                if (string.IsNullOrWhiteSpace(warning))
                {
                    throw new ArgumentException("Warning collection cannot contain empty values.", nameof(warnings));
                }

                copy.Add(warning);
            }

            return copy.AsReadOnly();
        }
    }
}
