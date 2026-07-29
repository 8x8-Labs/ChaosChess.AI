namespace ChaosChess.AI.Decision
{
    public interface ICardScorer
    {
        CardScore Score(CardScoringContext context);
    }
}
