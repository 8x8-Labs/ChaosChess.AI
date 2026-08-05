namespace ChaosChess.AI.Domain.CardCatalog
{
    public enum CardCatalogSupportGrade
    {
        ExactCommon = 0,
        CoarseOnly = 1,
        NeedsCommonPrimitive = 2,
        NeedsSpecialExtension = 3,
        DeferredUnsafe = 4
    }
}
