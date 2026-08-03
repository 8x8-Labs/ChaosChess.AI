namespace ChaosChess.AI.Domain.CardEffects
{
    public enum CardEffectApplicationCode
    {
        Success = 0,
        CoarseApplied = 1,
        UnsupportedEffect = 2,
        InvalidDefinition = 3,
        InvalidContext = 4,
        IllegalTarget = 5,
        StaleTarget = 6,
        RandomSourceMissing = 7,
        InvariantViolation = 8
    }
}
