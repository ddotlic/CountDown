﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using static CountDown.Solver;

namespace CountDown {
    public class BenchSolver {
        private static readonly List<long> _numbers = new List<long> { 7, 3, 4, 5, 15, 75 };
        private const long GOAL = 785;

        [Benchmark]
        public void SolveCountdown() => Solve(_numbers, GOAL);
    }

    public static class Program {
        private const int MAX_TOP = 20;

        // should be run with n1 n2 n3 n4 n5 n6 n7 where
        // n1 to n4 are one digit numbers
        // n5 is 10, 15, or 20
        // n6 is 25, 50, 75 or 100
        // n7 is the number to arrive at
        // example: using 1 1 4 7 15 50 522 should yield
        // 50 + (1 + 7) * (4 * 15 - 1) as the best result
        // good stress test example: 7 3 4 5 15 75 785
        // should yield 13 results in 175ms in debug mode
        public static void Main(string[] args) {
            if (args.Length < 7) {
                BenchmarkRunner.Run<BenchSolver>();
            } else {
                var numbers = new List<long>();
                for(int a = 0; a < args.Length; ++a)
                    numbers.Add(long.Parse(args[a]));
                var goal = numbers.Pop();
                var time = Stopwatch.StartNew();
                Solve(numbers, goal);
                time.Stop();
                var resultCount = Results.Count;
                var top = Math.Min(resultCount, MAX_TOP);
                var elapsed = time.ElapsedMilliseconds;
                Console.WriteLine(
                    $"Found {resultCount} results in {elapsed} ms, tried {Combinations} combinations.");
                Console.WriteLine($"Top {MAX_TOP} results (or less if there aren't as many)");
                for (int r = 0; r < top; ++r) {
                    Console.WriteLine(Results[r]);
                }
            }
        }
    }
}
