using System;

namespace ChaosChess.AI.Simulator
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return new SimulatorHost().Run(args, Console.Out, Console.Error);
        }
    }
}
