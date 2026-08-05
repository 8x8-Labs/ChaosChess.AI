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
                    new TileEffectCardTargetStrategy("at_mine", "AT Mine"),
                    new PawnMovementOverrideCardTargetStrategy("caterpillar", "Caterpillar"),
                    new ChargeCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("cobweb", "Cobweb"),
                    new PawnMovementOverrideCardTargetStrategy("concentration", "Concentration"),
                    new PieceValueCardTargetStrategy("dark_hand", "Dark Hand"),
                    new PawnMovementOverrideCardTargetStrategy("fast_march", "Fast March"),
                    new PieceValueCardTargetStrategy("gods_move", "God's Move"),
                    new TileEffectCardTargetStrategy("jumping_platform", "Jumping Platform"),
                    new PawnMovementOverrideCardTargetStrategy("limitless", "Limitless"),
                    new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion"),
                    new FireCardTargetStrategy(),
                    new PeaceZoneCardTargetStrategy(),
                    new PortalCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("psilocybin_mushroom", "Psilocybin Mushroom"),
                    new PawnMovementOverrideCardTargetStrategy("sneak_pawn", "Sneak Pawn"),
                    new PawnMovementOverrideCardTargetStrategy("thunderclap_flash", "Thunderclap Flash")
                });
        }

        public static CardTargetStrategyRegistry CreateRegistry()
        {
            return new CardTargetStrategyRegistry(CreateStrategies());
        }
    }
}
