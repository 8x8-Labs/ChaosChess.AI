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
                    new ArenaCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("at_mine", "AT Mine"),
                    new TileEffectCardTargetStrategy("blessing", "Blessing", TileEffectTargetProfile.Beneficial),
                    new CastleKnightCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy("caterpillar", "Caterpillar"),
                    new ChaoticKnightCardTargetStrategy(),
                    new ChargeCardTargetStrategy(),
                    new GlobalEffectCardTargetStrategy("checkmate_declaration", "Checkmate Declaration"),
                    new TileEffectCardTargetStrategy("cobweb", "Cobweb"),
                    new PawnMovementOverrideCardTargetStrategy("concentration", "Concentration"),
                    new PieceValueCardTargetStrategy("dark_hand", "Dark Hand"),
                    new GlobalEffectCardTargetStrategy("democracy", "Democracy"),
                    new PieceEffectCardTargetStrategy("desperado", "Desperado"),
                    new GlobalEffectCardTargetStrategy("destroyer_tank_cards", "Destroyer Tank Cards"),
                    new DimensionDisturbanceCardTargetStrategy(),
                    new PieceEffectCardTargetStrategy("dimension_instability", "Dimension Instability"),
                    new PieceEffectCardTargetStrategy("father_enemy", "Father Enemy"),
                    new PawnMovementOverrideCardTargetStrategy("fast_march", "Fast March"),
                    new GaslightingCardTargetStrategy(),
                    new PieceValueCardTargetStrategy("gods_move", "God's Move"),
                    new PieceEffectCardTargetStrategy("giant", "Giant"),
                    new HoneyTrapCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("jumping_platform", "Jumping Platform"),
                    new PawnMovementOverrideCardTargetStrategy("limitless", "Limitless"),
                    new MagnetCardTargetStrategy(),
                    new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion"),
                    new GlobalEffectCardTargetStrategy("mutiny", "Mutiny"),
                    new TileEffectCardTargetStrategy("obey_order", "Obey Order"),
                    new OverbearingCardTargetStrategy(),
                    new FireCardTargetStrategy(),
                    new PeaceZoneCardTargetStrategy(),
                    new PortalCardTargetStrategy(),
                    new PositionSwapCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("psilocybin_mushroom", "Psilocybin Mushroom"),
                    new RampartCardTargetStrategy(),
                    new ReviveCardTargetStrategy(),
                    new ShuffleBoardCardTargetStrategy(),
                    new PawnMovementOverrideCardTargetStrategy("sneak_pawn", "Sneak Pawn"),
                    new GlobalEffectCardTargetStrategy("stag_fight", "Stag Fight"),
                    new PieceEffectCardTargetStrategy("sunset_blade", "Sunset Blade", PieceEffectTargetProfile.CaptureSetup),
                    new SyncCardTargetStrategy(),
                    new TeleportCardTargetStrategy(),
                    new TileEffectCardTargetStrategy("time_bomb", "Time Bomb"),
                    new GlobalEffectCardTargetStrategy("time_reversal", "Time Reversal"),
                    new PawnMovementOverrideCardTargetStrategy("thunderclap_flash", "Thunderclap Flash"),
                    new TransmigrationCardTargetStrategy(),
                    new WeirdCastlingCardTargetStrategy(),
                    new GlobalEffectCardTargetStrategy("windmill", "Windmill")
                });
        }

        public static CardTargetStrategyRegistry CreateRegistry()
        {
            return new CardTargetStrategyRegistry(CreateStrategies());
        }
    }
}
