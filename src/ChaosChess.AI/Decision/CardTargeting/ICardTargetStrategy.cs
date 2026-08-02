namespace ChaosChess.AI.Decision.CardTargeting
{
    public interface ICardTargetStrategy
    {
        string CardId { get; }

        CardPlanDecisionResult Decide(CardTargetStrategyContext context);
    }
}
