using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

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
                    new PawnMovementOverrideCardTargetStrategy("caterpillar", "Caterpillar", new[] { PieceKind.Knight }),
                    new ChargeCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy(
                        "concentration",
                        "Concentration",
                        new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen }),
                    new PawnMovementOverrideCardTargetStrategy("fast_march", "Fast March"),
                    new PawnMovementOverrideCardTargetStrategy(
                        "limitless",
                        "Limitless",
                        new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen, PieceKind.King, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider }),
                    new FireCardTargetStrategy(),
                    new PeaceZoneCardTargetStrategy(),
                    new PortalCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy("sneak_pawn", "Sneak Pawn"),
                    new PawnMovementOverrideCardTargetStrategy("thunderclap_flash", "Thunderclap Flash", new[] { PieceKind.Rook })
                });
        }

        public static CardTargetStrategyRegistry CreateRegistry()
        {
            return new CardTargetStrategyRegistry(CreateStrategies());
        }
    }
}
