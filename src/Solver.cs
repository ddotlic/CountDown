using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CountDown {
    public enum Op { Add, Sub, Mul, Div };

    public abstract class Result {
        public long Total { get; protected set; }
        public abstract int Operations { get; }
        public override string ToString() => AsString(Op.Add);
        public abstract string AsString(Op parentOp);
    }

    public class ValRes : Result {
        public ValRes(long val) {
            Total = val;
        }

        public override int Operations { get => 0; }
        public override string AsString(Op parentOp) => Total.ToString();
    }

    public class AppRes : Result {
        private readonly Op _op;
        private readonly Result _left;
        private readonly Result _right;

        public AppRes(Op op, Result left, Result right, long total) {
            _op = op;
            _left = left;
            _right = right;
            Total = total;
        }

        public override int Operations { get => 1 + _left.Operations + _right.Operations; }

        public override string AsString(Op parentOp) {
            static int Priority(Op op) => op switch {
                Op.Add => 1,
                Op.Sub => 2,
                Op.Mul => 3,
                Op.Div => 3,
                _ => throw new ArgumentException("Operator not supported")
            };

            static string Operator(Op op) => op switch {
                Op.Add => "+",
                Op.Sub => "-",
                Op.Mul => "*",
                Op.Div => "/",
                _ => throw new ArgumentException("Operator not supported")
            };

            bool useParen = Priority(parentOp) > Priority(_op) || (parentOp == _op && _op == Op.Sub);
            var (start, end) = useParen ? ("(", ")") : (string.Empty, string.Empty);
            return $"{start}{_left.AsString(_op)} {Operator(_op)} {_right.AsString(_op)}{end}";
        }
    }

    public static class Solver {
        private static readonly Op[] _operations = { Op.Add, Op.Sub, Op.Mul, Op.Div };

        private static bool IsValid(Op op, long x, long y) {
            return op switch {
                Op.Add => x <= y,
                Op.Sub => x > y,
                Op.Mul => x != 1 && y != 1 && x <= y,
                Op.Div => y > 1 & ((x % y) == 0),
                _ => throw new ArgumentException("Operator not supported")
            };
        }

        private static long Apply(Op op, long a, long b) {
            return op switch {
                Op.Add => a + b,
                Op.Sub => a - b,
                Op.Mul => a * b,
                Op.Div => a / b,
                _ => throw new ArgumentException("Operator not supported")
            };
        }

        public static List<Result> Solve(List<long> numbers, long goal) {
            _cache.Clear();
            var results = new List<Result>();
            var candidates = numbers.Select(num => new ValRes(num)).Cast<Result>().ToList();
            SolveInternal(candidates, goal, results);
            return results.OrderBy(r => r.Operations).ToList();
        }

        public static int Combinations;

        private static Result Combine(Op op, Result x, Result y) {
            Combinations++;
            return new AppRes(op, x, y, Apply(op, x.Total, y.Total));
        }

        private static readonly HashSet<string> _cache = new HashSet<string>();
        private static readonly StringBuilder _builder = new StringBuilder(128);
        
        private static void SolveInternal(List<Result> candidates, long goal, List<Result> results) {
            if (candidates.Count <= 1) return;
            
            // TODO: this bit is not optimized, (necessary) lookup is too costly
            _builder.Clear();
            foreach(var c in candidates.OrderBy(c => c.Total)) {
                _builder.Append(c.Total.ToString("X2")).Append(',');
            }
            var hash = _builder.ToString();
            if (_cache.Contains(hash)) {
                 return;
            }
            _cache.Add(hash);
            
            for (int i = 0; i < candidates.Count; ++i) {
                for (int j = 0; j < candidates.Count; ++j) {
                    if (i == j) continue;
                    var x = candidates[i];
                    var y = candidates[j];
                    var combinations = _operations.Where(op => IsValid(op, x.Total, y.Total))
                        .Select(op => Combine(op, x, y));
                    foreach (var comb in combinations) {
                        if (comb.Total == goal) {
                            results.Add(comb);
                            continue;
                        }

                        if (candidates.Count == 2) continue;
                        var rest = candidates.Where((c, ix) => ix != i && ix != j).ToList();

                        rest.Add(comb);
                        SolveInternal(rest, goal, results);
                    }
                }
            }

        }
    }
}
