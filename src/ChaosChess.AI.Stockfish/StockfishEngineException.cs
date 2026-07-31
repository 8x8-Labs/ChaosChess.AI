using System;

namespace ChaosChess.AI.Stockfish
{
    public enum StockfishEngineErrorCode
    {
        HandshakeFailed,
        Timeout,
        InvalidOutput,
        ProcessExited
    }

    public sealed class StockfishEngineException : Exception
    {
        public StockfishEngineException(StockfishEngineErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public StockfishEngineErrorCode ErrorCode { get; }
    }
}
