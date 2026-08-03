using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulation;

namespace ChaosChess.AI.Simulator
{
    public sealed class HeadlessGameRunner
    {
        private const string CardSkippedReason = "not_applied_contract_missing";
        private readonly GameSimulator _simulator;

        public HeadlessGameRunner(GameSimulator simulator)
        {
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        }

        public HeadlessGameResult Run(
            GameState initialState,
            PieceColor perspective,
            HeadlessGameOptions? options = null)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }

            HeadlessGameOptions effectiveOptions = options ?? new HeadlessGameOptions();
            GameState currentState = initialState;
            int plyCount = 0;
            int cardsRecommended = 0;
            var warnings = new List<string>();
            var simulations = new List<SimulationResult>();

            while (plyCount < effectiveOptions.MaxPly)
            {
                var simulationOptions = new SimulationOptions(
                    effectiveOptions.SimulationHorizonPly,
                    effectiveOptions.VariationCount,
                    effectiveOptions.UseRandomTieBreak,
                    effectiveOptions.Seed);
                SimulationResult simulation = _simulator.SimulateFuture(
                    currentState,
                    perspective,
                    simulationOptions);

                simulations.Add(simulation);
                warnings.AddRange(simulation.Warnings);
                cardsRecommended += CountRecommendedCards(simulation);
                currentState = simulation.FinalState;
                plyCount += simulation.Steps.Count;

                if (simulation.TerminationReason == SimulationTerminationReason.HorizonReached)
                {
                    if (simulation.Steps.Count == 0)
                    {
                        return Invalid(
                            initialState,
                            currentState,
                            plyCount,
                            GameTerminationReason.InvalidTransition,
                            cardsRecommended,
                            warnings,
                            "horizon_reached_without_step",
                            simulations);
                    }

                    continue;
                }

                return Complete(
                    initialState,
                    currentState,
                    plyCount,
                    simulation.TerminationReason,
                    cardsRecommended,
                    warnings,
                    simulations);
            }

            return new HeadlessGameResult(
                initialState,
                currentState,
                plyCount,
                GameResult.Aborted,
                winner: null,
                GameTerminationReason.MaxPly,
                cardsRecommended,
                cardsApplied: 0,
                cardsRecommended > 0 ? CardSkippedReason : null,
                warnings,
                errorCode: "max_ply",
                simulations);
        }

        private static HeadlessGameResult Complete(
            GameState initialState,
            GameState finalState,
            int plyCount,
            SimulationTerminationReason reason,
            int cardsRecommended,
            IEnumerable<string> warnings,
            IEnumerable<SimulationResult> simulations)
        {
            PieceColor sideToMove = finalState.BoardState.SideToMove;

            switch (reason)
            {
                case SimulationTerminationReason.Checkmate:
                    PieceColor checkmateWinner = Opposite(sideToMove);
                    return Result(
                        initialState,
                        finalState,
                        plyCount,
                        checkmateWinner == PieceColor.White ? GameResult.WhiteWin : GameResult.BlackWin,
                        checkmateWinner,
                        GameTerminationReason.Checkmate,
                        cardsRecommended,
                        warnings,
                        errorCode: null,
                        simulations);
                case SimulationTerminationReason.Stalemate:
                    return Result(
                        initialState,
                        finalState,
                        plyCount,
                        GameResult.Draw,
                        winner: null,
                        GameTerminationReason.Stalemate,
                        cardsRecommended,
                        warnings,
                        errorCode: null,
                        simulations);
                case SimulationTerminationReason.KingRemoved:
                    PieceColor kingRemovedWinner = DetermineKingRemovedWinner(finalState) ?? Opposite(sideToMove);
                    return Result(
                        initialState,
                        finalState,
                        plyCount,
                        kingRemovedWinner == PieceColor.White ? GameResult.WhiteWin : GameResult.BlackWin,
                        kingRemovedWinner,
                        GameTerminationReason.KingRemoved,
                        cardsRecommended,
                        warnings,
                        errorCode: null,
                        simulations);
                case SimulationTerminationReason.NoEngineCandidates:
                    return Invalid(
                        initialState,
                        finalState,
                        plyCount,
                        GameTerminationReason.NoEngineCandidates,
                        cardsRecommended,
                        warnings,
                        "no_engine_candidates",
                        simulations);
                case SimulationTerminationReason.NoMoveRecommendations:
                    return Invalid(
                        initialState,
                        finalState,
                        plyCount,
                        GameTerminationReason.NoRecommendations,
                        cardsRecommended,
                        warnings,
                        "no_recommendations",
                        simulations);
                case SimulationTerminationReason.MoveBlocked:
                    return Invalid(
                        initialState,
                        finalState,
                        plyCount,
                        GameTerminationReason.MoveBlocked,
                        cardsRecommended,
                        warnings,
                        "move_blocked",
                        simulations);
                case SimulationTerminationReason.UnsupportedEffectEncountered:
                    return Invalid(
                        initialState,
                        finalState,
                        plyCount,
                        GameTerminationReason.UnsupportedEffect,
                        cardsRecommended,
                        warnings,
                        "unsupported_effect",
                        simulations);
                default:
                    return Invalid(
                        initialState,
                        finalState,
                        plyCount,
                        GameTerminationReason.InvalidTransition,
                        cardsRecommended,
                        warnings,
                        "invalid_transition",
                        simulations);
            }
        }

        private static HeadlessGameResult Result(
            GameState initialState,
            GameState finalState,
            int plyCount,
            GameResult result,
            PieceColor? winner,
            GameTerminationReason terminationReason,
            int cardsRecommended,
            IEnumerable<string> warnings,
            string? errorCode,
            IEnumerable<SimulationResult> simulations)
        {
            return new HeadlessGameResult(
                initialState,
                finalState,
                plyCount,
                result,
                winner,
                terminationReason,
                cardsRecommended,
                cardsApplied: 0,
                cardsRecommended > 0 ? CardSkippedReason : null,
                warnings,
                errorCode,
                simulations);
        }

        private static HeadlessGameResult Invalid(
            GameState initialState,
            GameState finalState,
            int plyCount,
            GameTerminationReason terminationReason,
            int cardsRecommended,
            IEnumerable<string> warnings,
            string errorCode,
            IEnumerable<SimulationResult> simulations)
        {
            return Result(
                initialState,
                finalState,
                plyCount,
                GameResult.Invalid,
                winner: null,
                terminationReason,
                cardsRecommended,
                warnings,
                errorCode,
                simulations);
        }

        private static int CountRecommendedCards(SimulationResult simulation)
        {
            int count = 0;

            foreach (SimulationStep step in simulation.Steps)
            {
                count += step.CardDecision.Recommendations.Count;
            }

            return count;
        }

        private static PieceColor? DetermineKingRemovedWinner(GameState finalState)
        {
            bool whiteKing = false;
            bool blackKing = false;

            foreach (PieceInfo piece in finalState.BoardState.Pieces)
            {
                if (piece.Kind != PieceKind.King)
                {
                    continue;
                }

                if (piece.Color == PieceColor.White)
                {
                    whiteKing = true;
                }
                else
                {
                    blackKing = true;
                }
            }

            if (whiteKing && !blackKing)
            {
                return PieceColor.White;
            }

            if (blackKing && !whiteKing)
            {
                return PieceColor.Black;
            }

            return null;
        }

        private static PieceColor Opposite(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }
    }
}
