using System;

namespace ChaosChess.AI.Domain
{
    public sealed class PositionEvaluation
    {
        public PositionEvaluation(
            PieceColor perspective,
            int? scoreCentipawns,
            int? mateIn)
        {
            if (perspective != PieceColor.White && perspective != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(perspective), perspective, "Unknown piece color.");
            }

            if (scoreCentipawns.HasValue == mateIn.HasValue)
            {
                throw new ArgumentException("Exactly one of centipawn score or mate distance must be supplied.");
            }

            if (mateIn == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mateIn), mateIn, "Predicted mate distance cannot be zero.");
            }

            Perspective = perspective;
            ScoreCentipawns = scoreCentipawns;
            MateIn = mateIn;
        }

        public PieceColor Perspective { get; }

        public int? ScoreCentipawns { get; }

        public int? MateIn { get; }
    }
}
