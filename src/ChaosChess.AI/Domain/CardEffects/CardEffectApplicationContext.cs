using System;
using ChaosChess.AI.Abstractions;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardEffectApplicationContext
    {
        public CardEffectApplicationContext(
            GameState state,
            CardUsePlan plan,
            PieceColor actor,
            PieceColor caster,
            PieceColor owner,
            IRandom? random = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            EnsureValidColor(actor, nameof(actor));
            EnsureValidColor(caster, nameof(caster));
            EnsureValidColor(owner, nameof(owner));

            if (plan.Actor != actor)
            {
                throw new ArgumentException("Application actor must match the CardUsePlan actor.", nameof(actor));
            }

            Actor = actor;
            Caster = caster;
            Owner = owner;
            Random = random;
        }

        public GameState State { get; }

        public CardUsePlan Plan { get; }

        public PieceColor Actor { get; }

        public PieceColor Caster { get; }

        public PieceColor Owner { get; }

        public IRandom? Random { get; }

        private static void EnsureValidColor(PieceColor color, string parameterName)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(parameterName, color, "Unknown piece color.");
            }
        }
    }
}
