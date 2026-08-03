using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public interface ICardEffectPlanningProbe
    {
        CardEffectPlanningResult Probe(
            GameState gameState,
            CardInfo card,
            CardUsePlan plan);
    }
}
