namespace ChaosChess.AI.Domain
{
    public sealed class CardEffectParameters
    {
        private CardEffectParameters()
        {
        }

        public static CardEffectParameters Empty { get; } = new CardEffectParameters();
    }
}
