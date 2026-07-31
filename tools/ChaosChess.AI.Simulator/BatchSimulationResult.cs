using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Simulator
{
    public sealed class BatchSimulationResult
    {
        private readonly ReadOnlyCollection<BatchGameResult> _games;

        public BatchSimulationResult(BatchSimulationOptions options, IEnumerable<BatchGameResult> games)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _games = CopyGames(games);
            ValidGameCount = Count(GameResult.WhiteWin) + Count(GameResult.BlackWin) + Count(GameResult.Draw);
            InvalidGameCount = Count(GameResult.Invalid);
            AbortedGameCount = Count(GameResult.Aborted);
        }

        public BatchSimulationOptions Options { get; }

        public IReadOnlyList<BatchGameResult> Games => _games;

        public int ValidGameCount { get; }

        public int InvalidGameCount { get; }

        public int AbortedGameCount { get; }

        private int Count(GameResult result)
        {
            int count = 0;

            foreach (BatchGameResult game in _games)
            {
                if (game.GameResult.Result == result)
                {
                    count++;
                }
            }

            return count;
        }

        private static ReadOnlyCollection<BatchGameResult> CopyGames(IEnumerable<BatchGameResult> games)
        {
            if (games == null)
            {
                throw new ArgumentNullException(nameof(games));
            }

            var copy = new List<BatchGameResult>();

            foreach (BatchGameResult game in games)
            {
                if (game == null)
                {
                    throw new ArgumentException("Game result collection cannot contain null.", nameof(games));
                }

                copy.Add(game);
            }

            return copy.AsReadOnly();
        }
    }
}
