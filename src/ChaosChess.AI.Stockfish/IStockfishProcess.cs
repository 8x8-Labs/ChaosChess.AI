using System;

namespace ChaosChess.AI.Stockfish
{
    public interface IStockfishProcess : IDisposable
    {
        bool HasExited { get; }

        void Start();

        void WriteLine(string command);

        string? ReadLine(TimeSpan timeout);

        void ClearOutput();

        void Kill();
    }
}
