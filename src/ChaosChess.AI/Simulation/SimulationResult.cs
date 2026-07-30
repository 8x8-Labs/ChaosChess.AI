using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulation
{
    public sealed class SimulationResult
    {
        private readonly ReadOnlyCollection<SimulationStep> _steps;
        private readonly ReadOnlyCollection<string> _warnings;

        public SimulationResult(
            GameState initialState,
            GameState finalState,
            int? seed,
            int horizonPly,
            IEnumerable<SimulationStep> steps,
            SimulationTerminationReason terminationReason,
            IEnumerable<string> warnings)
        {
            if (horizonPly < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(horizonPly), horizonPly, "Horizon ply cannot be negative.");
            }

            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
            Seed = seed;
            HorizonPly = horizonPly;
            _steps = CopySteps(steps);
            TerminationReason = terminationReason;
            _warnings = CopyWarnings(warnings);
        }

        public GameState InitialState { get; }

        public GameState FinalState { get; }

        public int? Seed { get; }

        public int HorizonPly { get; }

        public IReadOnlyList<SimulationStep> Steps => _steps;

        public SimulationTerminationReason TerminationReason { get; }

        public IReadOnlyList<string> Warnings => _warnings;

        private static ReadOnlyCollection<SimulationStep> CopySteps(IEnumerable<SimulationStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            var copy = new List<SimulationStep>();

            foreach (SimulationStep step in steps)
            {
                if (step == null)
                {
                    throw new ArgumentException("Step collection cannot contain null.", nameof(steps));
                }

                copy.Add(step);
            }

            return copy.AsReadOnly();
        }

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
