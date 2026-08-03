using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Fen;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceScenarioGameStateFactory
    {
        public static GameState Create(BalanceSimulationScenario scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            return new GameState(
                FenParser.Parse(scenario.StartingFen),
                CreateCards(scenario.Cards),
                CreateTileEffects(scenario.TileEffects));
        }

        private static IReadOnlyList<CardInfo> CreateCards(
            IEnumerable<BalanceScenarioCard> scenarioCards)
        {
            var cards = new List<CardInfo>();

            foreach (BalanceScenarioCard card in scenarioCards)
            {
                cards.Add(new CardInfo(
                    card.CardId,
                    card.Category,
                    card.RemainingUses));
            }

            return cards.AsReadOnly();
        }

        private static IReadOnlyList<TileEffectInfo> CreateTileEffects(
            IEnumerable<BalanceScenarioTileEffect> scenarioTileEffects)
        {
            var tileEffects = new List<TileEffectInfo>();

            foreach (BalanceScenarioTileEffect tileEffect in scenarioTileEffects)
            {
                tileEffects.Add(new TileEffectInfo(
                    tileEffect.Id,
                    tileEffect.EffectType,
                    tileEffect.Square,
                    tileEffect.Owner,
                    tileEffect.RemainingTurns,
                    tileEffect.DestinationSquare,
                    tileEffect.SharedRemainingUses));
            }

            return tileEffects.AsReadOnly();
        }
    }
}
