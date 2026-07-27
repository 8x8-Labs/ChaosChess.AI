namespace ChaosChess.AI.Evaluation
{
    public sealed class EvaluationResult
    {
        public EvaluationResult(
            int material,
            int threat,
            int advantage,
            int kingSafety,
            int totalScore)
        {
            Material = material;
            Threat = threat;
            Advantage = advantage;
            KingSafety = kingSafety;
            TotalScore = totalScore;
        }

        public int Material { get; }

        public int Threat { get; }

        public int Advantage { get; }

        public int KingSafety { get; }

        public int TotalScore { get; }
    }
}
