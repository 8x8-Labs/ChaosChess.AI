using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Abstractions
{
    public interface IChessEngine
    {
        IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount);

        bool IsInCheck(BoardState boardState);
    }
}
