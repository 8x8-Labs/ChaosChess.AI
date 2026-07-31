using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;

namespace ChaosChess.AI.Simulation
{
    public sealed class SimulationStep
    {
        private readonly ReadOnlyCollection<string> _warnings;

        public SimulationStep(
            int plyIndex,
            PieceColor sideToMove,
            GameState stateBefore,
            EvaluationResult evaluationBefore,
            CardDecisionResult cardDecision,
            MoveFilterResult moveFilterResult,
            MoveRecommendation? selectedMove,
            GameState stateAfter,
            SimulationTerminationReason? terminationReason,
            IEnumerable<string> warnings)
        {
            if (plyIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plyIndex), plyIndex, "Ply index cannot be negative.");
            }

            PlyIndex = plyIndex;
            SideToMove = sideToMove;
            StateBefore = stateBefore ?? throw new ArgumentNullException(nameof(stateBefore));
            EvaluationBefore = evaluationBefore ?? throw new ArgumentNullException(nameof(evaluationBefore));
            CardDecision = cardDecision ?? throw new ArgumentNullException(nameof(cardDecision));
            MoveFilterResult = moveFilterResult ?? throw new ArgumentNullException(nameof(moveFilterResult));
            SelectedMove = selectedMove;
            StateAfter = stateAfter ?? throw new ArgumentNullException(nameof(stateAfter));
            TerminationReason = terminationReason;
            _warnings = CopyWarnings(warnings);
        }

        public int PlyIndex { get; }

        public PieceColor SideToMove { get; }

        public GameState StateBefore { get; }

        public EvaluationResult EvaluationBefore { get; }

        public CardDecisionResult CardDecision { get; }

        public MoveFilterResult MoveFilterResult { get; }

        public MoveRecommendation? SelectedMove { get; }

        public string? SelectedUciMove => SelectedMove?.UciMove;

        public GameState StateAfter { get; }

        public SimulationTerminationReason? TerminationReason { get; }

        public IReadOnlyList<string> Warnings => _warnings;

        private static ReadOnlyCollection<string> CopyWarnings(IEnumerable<string> warnings)
        {
            if (warnings == null)
            {
                throw new ArgumentNullException(nameof(warnings));
            }

            var copy = new List<string>();

            foreach (string warning in warnings)
            {
                if (string.IsNullOrWhiteSpace(warning))
                {
                    throw new ArgumentException("Warning collection cannot contain empty values.", nameof(warnings));
                }

                copy.Add(warning);
            }

            return copy.AsReadOnly();
        }
    }
}
