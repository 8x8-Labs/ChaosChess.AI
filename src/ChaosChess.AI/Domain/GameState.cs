using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class GameState
    {
        private readonly ReadOnlyCollection<CardInfo> _availableCards;
        private readonly ReadOnlyCollection<TileEffectInfo> _tileEffects;

        public GameState(
            BoardState boardState,
            IEnumerable<CardInfo> availableCards,
            IEnumerable<TileEffectInfo> tileEffects)
        {
            BoardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _availableCards = CopyCards(availableCards);
            _tileEffects = CopyTileEffects(tileEffects);
        }

        public BoardState BoardState { get; }

        public IReadOnlyList<CardInfo> AvailableCards => _availableCards;

        public IReadOnlyList<TileEffectInfo> TileEffects => _tileEffects;

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
    }
}
