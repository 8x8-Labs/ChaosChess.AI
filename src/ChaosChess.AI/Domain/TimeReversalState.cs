using System;

namespace ChaosChess.AI.Domain
{
    public sealed class TimeReversalState
    {
        public TimeReversalState(
            string id,
            PieceColor owner,
            int remainingTurns,
            BoardState savedBoardState)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Time reversal ID cannot be empty.", nameof(id));
            }

            if (owner != PieceColor.White && owner != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(owner), owner, "Unsupported piece color.");
            }

            if (remainingTurns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingTurns), remainingTurns, "Remaining turns cannot be negative.");
            }

            Id = id;
            Owner = owner;
            RemainingTurns = remainingTurns;
            SavedBoardState = savedBoardState ?? throw new ArgumentNullException(nameof(savedBoardState));
        }

        public string Id { get; }

        public PieceColor Owner { get; }

        public int RemainingTurns { get; }

        public BoardState SavedBoardState { get; }

        public TimeReversalState Tick()
        {
            if (RemainingTurns == 0)
            {
                return this;
            }

            return new TimeReversalState(Id, Owner, RemainingTurns - 1, SavedBoardState);
        }
    }
}
