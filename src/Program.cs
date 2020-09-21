using System;
using System.Collections.Generic;
using System.Diagnostics;
using static CountDown.Solver;

namespace CountDown {
    public static class Program {
        public static (List<long>, long) ParseArgs(string[] args) {
            var numbers = new List<long>();
            for (int a = 0; a < 6; ++a)
                numbers.Add(long.Parse(args[a]));
            var goal = long.Parse(args[6]);
            return (numbers, goal);
        }
        private const int _maxTop = 5;
#if DEBUG
        private const int _turns = 10;
#else        
        private const int _turns = 250;
#endif
        
        // few results: using 1 1 4 7 15 50 522 should yield
        // 50 + (1 + 7) * (4 * 15 - 1) as the best result (and one more)
        // good stress test: using 7 3 4 5 15 75 785 should yield
        // 13 results using around 120000 tries
        public static void Main(string[] args) {
            var numArgs = args.Length;
            if (numArgs != 7) {
                Console.WriteLine("Parameters: n1 n2 n3 n4 n5 n6 n7");
                Console.WriteLine("n1 to n4 are one digit numbers");
                Console.WriteLine("n5 is 10, 15, or 20");
                Console.WriteLine("n6 is 25, 50, 75 or 100");
                Console.WriteLine("n7 is a three digit number to arrive at");
                return;
            }

            var (numbers, goal) = ParseArgs(args);
            var time = Stopwatch.StartNew();
            for (int p = 0; p < _turns; ++p) {
                Solve(numbers, goal);
            }
            time.Stop();
            var numResults = Results.Count;
            var top = Math.Min(numResults, _maxTop);
            var elapsed = Math.Round(time.ElapsedMilliseconds / (double)_turns, 2);
            Console.WriteLine(
                $"Found {numResults} results with {_turns} turns in {elapsed} ms; tried {Combinations} combinations.");
            Console.WriteLine($"Top {_maxTop} results (or less if there aren't as many)");
            for (int r = 0; r < top; ++r) {
                Console.WriteLine(Results[r]);
            }
        }
    }
}
