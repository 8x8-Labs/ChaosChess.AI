using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanCandidateEnumerator
    {
        private static readonly CardPlanScore NeutralScore = new CardPlanScore(
            0,
            new[]
            {
                new CardPlanScoreComponent(
                    "enumeration.neutral",
                    0,
                    "Candidate was generated without card-specific scoring.")
            });

        private readonly DefaultCardPlanningCatalog _planningCatalog;
        private readonly CardUsePlanValidator _validator;

        public CardPlanCandidateEnumerator()
            : this(new DefaultCardPlanningCatalog())
        {
        }

        public CardPlanCandidateEnumerator(DefaultCardPlanningCatalog planningCatalog)
        {
            _planningCatalog = planningCatalog ?? throw new ArgumentNullException(nameof(planningCatalog));
            _validator = new CardUsePlanValidator(_planningCatalog);
        }

        public IReadOnlyList<CardPlanCandidate> EnumerateLegalCandidates(
            GameState gameState,
            CardInfo card,
            PieceColor actor)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            EnsureValidColor(actor);

            CardPlanningDefinition definition = _planningCatalog.GetDefinition(card.Id);
            if (!definition.IsSupported)
            {
                return Array.Empty<CardPlanCandidate>();
            }

            var candidates = new List<CardPlanCandidate>();
            int enumerationIndex = 0;

            foreach (CardTargetSelection target in EnumerateTargets(gameState, actor, definition))
            {
                var plan = new CardUsePlan(card.Id, actor, target);
                CardPlanValidationResult validation = _validator.Validate(gameState, plan);

                if (!validation.IsValid)
                {
                    continue;
                }

                candidates.Add(new CardPlanCandidate(
                    card,
                    plan,
                    NeutralScore,
                    enumerationIndex));
                enumerationIndex++;
            }

            return candidates.AsReadOnly();
        }

        private static IEnumerable<CardTargetSelection> EnumerateTargets(
            GameState gameState,
            PieceColor actor,
            CardPlanningDefinition definition)
        {
            switch (definition.RequiredTargetKind)
            {
                case CardTargetKind.None:
                    yield return CardTargetSelection.None();
                    yield break;

                case CardTargetKind.PieceAtSquare:
                    foreach (PieceInfo piece in gameState.BoardState.Pieces)
                    {
                        if (!MatchesOwnerRelation(definition.RequiredTargetOwnerRelation, actor, piece.Color) ||
                            !IsAllowedPieceKind(definition.AllowedTargetPieceKinds, piece.Kind))
                        {
                            continue;
                        }

                        yield return CardTargetSelection.PieceAtSquare(
                            new PieceTargetSnapshot(piece.Square, piece.Color, piece.Kind));
                    }

                    yield break;

                case CardTargetKind.BoardSquare:
                    foreach (Square square in EnumerateBoardSquares())
                    {
                        yield return CardTargetSelection.BoardSquare(square);
                    }

                    yield break;

                case CardTargetKind.OrderedSquares:
                    Square[] squares = CopySquares(EnumerateBoardSquares());
                    for (int firstIndex = 0; firstIndex < squares.Length; firstIndex++)
                    {
                        for (int secondIndex = 0; secondIndex < squares.Length; secondIndex++)
                        {
                            if (firstIndex == secondIndex)
                            {
                                continue;
                            }

                            yield return CardTargetSelection.OrderedSquares(
                                new[] { squares[firstIndex], squares[secondIndex] });
                        }
                    }

                    yield break;
            }
        }

        private static bool MatchesOwnerRelation(
            CardTargetOwnerRelation relation,
            PieceColor actor,
            PieceColor target)
        {
            switch (relation)
            {
                case CardTargetOwnerRelation.Self:
                    return target == actor;
                case CardTargetOwnerRelation.Opponent:
                    return target != actor;
                case CardTargetOwnerRelation.Any:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAllowedPieceKind(
            IReadOnlyList<PieceKind> allowedKinds,
            PieceKind kind)
        {
            if (allowedKinds.Count == 0)
            {
                return true;
            }

            foreach (PieceKind allowedKind in allowedKinds)
            {
                if (allowedKind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Square> EnumerateBoardSquares()
        {
            for (int rank = 0; rank < Square.BoardSize; rank++)
            {
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    yield return new Square(file, rank);
                }
            }
        }

        private static Square[] CopySquares(IEnumerable<Square> squares)
        {
            var copy = new List<Square>();

            foreach (Square square in squares)
            {
                copy.Add(square);
            }

            return copy.ToArray();
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
