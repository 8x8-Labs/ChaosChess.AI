using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceSimulationScenario
    {
        private readonly ReadOnlyCollection<BalanceScenarioCard> _cards;
        private readonly ReadOnlyCollection<BalanceScenarioTileEffect> _tileEffects;

        public BalanceSimulationScenario(
            string scenarioId,
            int schemaVersion,
            string startingFen,
            PieceColor actor,
            IEnumerable<BalanceScenarioCard>? cards,
            IEnumerable<BalanceScenarioTileEffect>? tileEffects,
            BalanceEngineObservation? engineObservation,
            string scenarioGroup,
            BalanceExpectedBehavior expectedBehavior = BalanceExpectedBehavior.Unspecified)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                throw new ArgumentException("Scenario id cannot be empty.", nameof(scenarioId));
            }

            if (schemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "Schema version must be positive.");
            }

            if (string.IsNullOrWhiteSpace(startingFen))
            {
                throw new ArgumentException("Starting FEN cannot be empty.", nameof(startingFen));
            }

            if (actor != PieceColor.White && actor != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(actor), actor, "Unknown actor color.");
            }

            if (string.IsNullOrWhiteSpace(scenarioGroup))
            {
                throw new ArgumentException("Scenario group cannot be empty.", nameof(scenarioGroup));
            }

            ScenarioId = scenarioId;
            SchemaVersion = schemaVersion;
            StartingFen = startingFen;
            Actor = actor;
            _cards = CopyCards(cards);
            _tileEffects = CopyTileEffects(tileEffects);
            EngineObservation = engineObservation ?? new BalanceEngineObservation();
            ScenarioGroup = scenarioGroup;
            ExpectedBehavior = expectedBehavior;
        }

        public string ScenarioId { get; }

        public int SchemaVersion { get; }

        public string StartingFen { get; }

        public PieceColor Actor { get; }

        public IReadOnlyList<BalanceScenarioCard> Cards => _cards;

        public IReadOnlyList<BalanceScenarioTileEffect> TileEffects => _tileEffects;

        public BalanceEngineObservation EngineObservation { get; }

        public string ScenarioGroup { get; }

        public BalanceExpectedBehavior ExpectedBehavior { get; }

        private static ReadOnlyCollection<BalanceScenarioCard> CopyCards(
            IEnumerable<BalanceScenarioCard>? cards)
        {
            var copy = new List<BalanceScenarioCard>();

            if (cards == null)
            {
                return copy.AsReadOnly();
            }

            foreach (BalanceScenarioCard card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Card collection cannot contain null.", nameof(cards));
                }

                copy.Add(card);
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<BalanceScenarioTileEffect> CopyTileEffects(
            IEnumerable<BalanceScenarioTileEffect>? tileEffects)
        {
            var copy = new List<BalanceScenarioTileEffect>();

            if (tileEffects == null)
            {
                return copy.AsReadOnly();
            }

            foreach (BalanceScenarioTileEffect tileEffect in tileEffects)
            {
                if (tileEffect == null)
                {
                    throw new ArgumentException("Tile effect collection cannot contain null.", nameof(tileEffects));
                }

                copy.Add(tileEffect);
            }

            return copy.AsReadOnly();
        }
    }
}
