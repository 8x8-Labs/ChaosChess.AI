using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain.CardCatalog
{
    public sealed class DefaultCardCatalog
    {
        private static readonly CardCatalogEntry[] DefaultEntries =
        {
            Entry("agile", "AgileCard.asset", "AgileCard", UnityCardType.Piece, true, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave0, "movement-override-state", "Existing Unity AI card; current applier reports movement overrides as unsupported."),
            Entry("aim", "AimCard.asset", "AimCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave1, "movement-override-state", "Pawn movement override; supported for planning/execution with report-only effect simulation."),
            Entry("arena", "ArenaCard.asset", "ArenaCard", UnityCardType.Global, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "global-rule-extension", "Global arena flow with random selection."),
            Entry("at_mine", "ATMineCard.asset", "ATMineCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger", "Tile trigger effect."),
            Entry("blessing", "BlessingCard.asset", "BlessingCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger-piece-transform", "Tile trigger with piece transformation."),
            Entry("castle_knight", "CastleKnight.asset", "CastleKnight", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "change-piece-kind", "Piece transformation."),
            Entry("caterpillar", "CaterpillarCard.asset", "CaterpillarCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("chaotic_knight", "ChaoticKnightCard.asset", "ChaoticKnightCard", UnityCardType.Piece, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized piece behavior."),
            Entry("charge", "ChargeCard.asset", "ChargeCard", UnityCardType.Global, true, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave0, "derived-move-piece", "Existing Unity AI card; current effect definition has no explicit source or destination."),
            Entry("checkmate_declaration", "CheckmateDeclarationCard.asset", "CheckmateDeclarationCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "conditional-piece-removal", "Conditional global effect."),
            Entry("cobweb", "CobwebCard.asset", "CobwebCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger-movement", "Tile trigger with movement side effects."),
            Entry("concentration", "ConcentrationCard.asset", "ConcentrationCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("dark_hand", "DarkHandCard.asset", "DarkHandCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("democracy", "DemocracyCard.asset", "DemocracyCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "global-owner-policy", "Global target color effect."),
            Entry("desperado", "DesperadoCard.asset", "DesperadoCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "remove-piece", "Piece removal effect."),
            Entry("destroyer_tank_cards", "DestroyerTankCards.asset", "DestroyerTankCards", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "global-piece-effect", "Global piece effect."),
            Entry("dimension_disturbance", "DimensionDisturbanceCard.asset", "DimensionDisturbanceCard", UnityCardType.Piece, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized piece removal."),
            Entry("dimension_instability", "DimensionInstabilityCard.asset", "DimensionInstabilityCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "special-movement-state", "Complex piece state change."),
            Entry("fast_march", "FastMarchCard.asset", "FastMarchCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave1, "movement-override-state", "Pawn movement override; supported for planning/execution with report-only effect simulation."),
            Entry("father_enemy", "FatherEnemyCard.asset", "FatherEnemyCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "change-owner", "Piece owner relation effect."),
            Entry("fire", "FireCard.asset", "FireCard", UnityCardType.Tile, true, CardCatalogSupportGrade.ExactCommon, CardCatalogActivationWave.Wave0, "none", "Existing Unity AI card; persistent tile effect can be represented exactly for post-card planning."),
            Entry("gaslighting", "GaslightingCard.asset", "GaslightingCard", UnityCardType.Piece, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized owner or piece effect."),
            Entry("giant", "GiantCard.asset", "GiantCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "piece-size-stun-state", "Special piece state and stun effect."),
            Entry("gods_move", "GodsMoveCard.asset", "GodsMoveCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "move-piece", "Piece movement effect."),
            Entry("honey_trap", "HoneyTrapCard.asset", "HoneyTrapCard", UnityCardType.Piece, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized piece effect."),
            Entry("jumping_platform", "JumpingPlatformCard.asset", "JumpingPlatformCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger-movement", "Tile trigger movement effect."),
            Entry("limitless", "LimitlessCard.asset", "LimitlessCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("magnet", "MagnetCard.asset", "MagnetCard", UnityCardType.Tile, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized tile movement effect."),
            Entry("missing_promotion", "MissingPromotionCard.asset", "MissingPromotionCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "change-piece-kind", "Piece promotion effect."),
            Entry("mutiny", "MutinyCard.asset", "MutinyCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "change-owner", "Piece owner relation effect."),
            Entry("obey_order", "ObeyOrderCard.asset", "ObeyOrderCard", UnityCardType.Tile, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-source-policy", "Randomized tile movement effect."),
            Entry("overbearing", "OverbearingCard.asset", "OverbearingCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "global-movement-state", "Global movement effect."),
            Entry("peace_zone", "PeaceZoneCard.asset", "PeaceZoneCard", UnityCardType.Tile, true, CardCatalogSupportGrade.ExactCommon, CardCatalogActivationWave.Wave0, "none", "Existing Unity AI card; persistent tile effect can be represented exactly for post-card planning."),
            Entry("portal", "PortalCard.asset", "PortalCard", UnityCardType.Tile, true, CardCatalogSupportGrade.ExactCommon, CardCatalogActivationWave.Wave0, "none", "Existing Unity AI card; persistent linked endpoints can be represented exactly for post-card planning."),
            Entry("position_swap", "PositionSwapCard.asset", "PositionSwapCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "multi-piece-move", "Global position swap effect."),
            Entry("psilocybin_mushroom", "PsilocybinMushroomCard.asset", "PsilocybinMushroomCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger-movement-override", "Tile trigger movement override."),
            Entry("rampart", "RampartCard.asset", "RampartCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "create-piece-or-blocker", "Board blocker creation effect."),
            Entry("revive", "ReviveCard.asset", "ReviveCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "tile-trigger-create-piece", "Tile trigger with piece creation."),
            Entry("shuffle_board", "ShuffleBoardCard.asset", "ShuffleBoardCard", UnityCardType.Global, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "random-board-mutation", "Random full-board mutation."),
            Entry("sneak_pawn", "SneakPawnCard.asset", "SneakPawnCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("stag_fight", "StagFightCard.asset", "StagFightCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "global-forced-move", "Global forced move effect."),
            Entry("sunset_blade", "SunsetBlade.asset", "SunsetBlade", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "remove-piece", "Piece removal effect."),
            Entry("sync", "SyncCard.asset", "SyncCard", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "linked-tile-trigger", "Complex linked tile effect."),
            Entry("teleport", "TeleportCard.asset", "TeleportCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "move-piece", "Piece-to-square movement effect."),
            Entry("thunderclap_flash", "ThunderclapFlashCard.asset", "ThunderclapFlashCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "movement-override-state", "Piece movement override."),
            Entry("time_bomb", "TimeBomb.asset", "TimeBomb", UnityCardType.Tile, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "delayed-tile-trigger", "Delayed tile trigger effect."),
            Entry("time_reversal", "TimeReversalCard.asset", "TimeReversalCard", UnityCardType.Global, false, CardCatalogSupportGrade.DeferredUnsafe, CardCatalogActivationWave.Deferred, "history-state", "Requires board history rollback."),
            Entry("transmigration", "TransmigrationCard.asset", "TransmigrationCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsCommonPrimitive, CardCatalogActivationWave.Wave2, "change-piece-kind", "Piece transformation effect."),
            Entry("weird_castling", "WeirdCastlingCard.asset", "WeirdCastlingCard", UnityCardType.Piece, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "special-castling-move", "Special castling-like movement."),
            Entry("windmill", "WindmillCard.asset", "WindmillCard", UnityCardType.Global, false, CardCatalogSupportGrade.NeedsSpecialExtension, CardCatalogActivationWave.Wave3, "global-movement-override", "Global movement effect.")
        };

        private readonly IReadOnlyDictionary<string, CardCatalogEntry> _entries;

        public DefaultCardCatalog()
            : this(DefaultEntries)
        {
        }

        public DefaultCardCatalog(IEnumerable<CardCatalogEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var copy = new Dictionary<string, CardCatalogEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (CardCatalogEntry entry in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Entry collection cannot contain null.", nameof(entries));
                }

                copy.Add(entry.CardId, entry);
            }

            _entries = new ReadOnlyDictionary<string, CardCatalogEntry>(copy);
        }

        public IReadOnlyDictionary<string, CardCatalogEntry> Entries => _entries;

        public CardCatalogEntry? FindEntry(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _entries.TryGetValue(cardId, out CardCatalogEntry entry)
                ? entry
                : null;
        }

        public bool TryGetEntry(string cardId, out CardCatalogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _entries.TryGetValue(cardId, out entry);
        }

        private static CardCatalogEntry Entry(
            string cardId,
            string unityAssetName,
            string displayName,
            UnityCardType unityType,
            bool currentUnityAiSupported,
            CardCatalogSupportGrade supportGrade,
            CardCatalogActivationWave activationWave,
            string primitiveGap,
            string notes)
        {
            return new CardCatalogEntry(
                cardId,
                unityAssetName,
                displayName,
                unityType,
                currentUnityAiSupported,
                supportGrade,
                activationWave,
                primitiveGap,
                notes);
        }
    }
}
