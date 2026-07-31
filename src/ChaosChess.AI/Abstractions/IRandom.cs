namespace ChaosChess.AI.Abstractions
{
    public interface IRandom
    {
        int NextInt(int minInclusive, int maxExclusive);

        double NextDouble();
    }
}
