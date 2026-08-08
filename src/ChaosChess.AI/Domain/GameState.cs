using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class GameState
    {
        private readonly ReadOnlyCollection<CardInfo> _availableCards;
        private readonly ReadOnlyCollection<TileEffectInfo> _tileEffects;
        private readonly ReadOnlyCollection<TimeReversalState> _timeReversals;

        public GameState(
            BoardState boardState,
            IEnumerable<CardInfo> availableCards,
            IEnumerable<TileEffectInfo> tileEffects,
            CapturedPieceState? capturedPieces = null,
            IEnumerable<TimeReversalState>? timeReversals = null)
        {
            BoardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _availableCards = CopyCards(availableCards);
            _tileEffects = CopyTileEffects(tileEffects);
            CapturedPieces = capturedPieces ?? CapturedPieceState.Empty;
            _timeReversals = CopyTimeReversals(timeReversals);
        }

        public BoardState BoardState { get; }

        public IReadOnlyList<CardInfo> AvailableCards => _availableCards;

        public IReadOnlyList<TileEffectInfo> TileEffects => _tileEffects;

        public CapturedPieceState CapturedPieces { get; }

        public IReadOnlyList<TimeReversalState> TimeReversals => _timeReversals;

        private static ReadOnlyCollection<CardInfo> CopyCards(IEnumerable<CardInfo> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var copy = new List<CardInfo>();

            foreach (CardInfo card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Card collection cannot contain null.", nameof(cards));
                }

                copy.Add(card);
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<TileEffectInfo> CopyTileEffects(IEnumerable<TileEffectInfo> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var copy = new List<TileEffectInfo>();

            foreach (TileEffectInfo effect in effects)
            {
                if (effect == null)
                {
                    throw new ArgumentException("Tile effect collection cannot contain null.", nameof(effects));
                }

                copy.Add(effect);
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<TimeReversalState> CopyTimeReversals(IEnumerable<TimeReversalState>? effects)
        {
            var copy = new List<TimeReversalState>();

            if (effects == null)
            {
                return copy.AsReadOnly();
            }

            foreach (TimeReversalState effect in effects)
            {
                if (effect == null)
                {
                    throw new ArgumentException("Time reversal collection cannot contain null.", nameof(effects));
                }

                copy.Add(effect);
            }

            return copy.AsReadOnly();
        }
    }
}
