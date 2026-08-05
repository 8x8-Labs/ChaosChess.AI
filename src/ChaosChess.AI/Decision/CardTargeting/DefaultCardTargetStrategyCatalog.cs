using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public static class DefaultCardTargetStrategyCatalog
    {
        public static IReadOnlyList<ICardTargetStrategy> CreateStrategies()
        {
            return new ReadOnlyCollection<ICardTargetStrategy>(
                new ICardTargetStrategy[]
                {
                    new AgileCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy("aim", "Aim"),
                    new ChargeCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy("fast_march", "Fast March"),
                    new FireCardTargetStrategy(),
                    new PeaceZoneCardTargetStrategy(),
                    new PortalCardTargetStrategy()
                });
        }

        public static CardTargetStrategyRegistry CreateRegistry()
        {
            return new CardTargetStrategyRegistry(CreateStrategies());
        }
    }
}
