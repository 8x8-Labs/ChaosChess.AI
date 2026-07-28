namespace ChaosChess.AI.Evaluation
{
    public sealed class EvaluationResult
    {
        public EvaluationResult(
            int boardScore,
            int? mateIn,
            int threat,
            int advantage,
            int totalScore)
        {
            BoardScore = boardScore;
            MateIn = mateIn;
            Threat = threat;
            Advantage = advantage;
            TotalScore = totalScore;
        }

        public int BoardScore { get; }

        public int? MateIn { get; }

        public int Threat { get; }

        public int Advantage { get; }

        public int TotalScore { get; }
    }
}
