using System;

namespace ChaosChess.AI.Domain
{
    public enum TileEffectLifetimeKind
    {
        TurnLimited = 0,
        PersistentUntilTriggered = 1
    }

    internal static class TileEffectLifetimeKindGuard
    {
        public static void EnsureValid(TileEffectLifetimeKind lifetimeKind, string parameterName)
        {
            if (lifetimeKind != TileEffectLifetimeKind.TurnLimited &&
                lifetimeKind != TileEffectLifetimeKind.PersistentUntilTriggered)
            {
                throw new ArgumentOutOfRangeException(parameterName, lifetimeKind, "Unknown tile effect lifetime kind.");
            }
        }
    }
}
