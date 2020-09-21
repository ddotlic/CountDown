using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

        private static readonly long[] _priority = { 1, 2, 3, 3 };
        private static readonly string[] _operator = { "+", "-", "*", "/" };
        private static readonly (string, string) _parens = ("(", ")");
        private static readonly (string, string) _nothing = (string.Empty, string.Empty);

        public override string AsString(Op parentOp) {
            int op = (int)_op;
            bool useParen = _priority[(int)parentOp] > _priority[op] || (parentOp == _op && _op == Op.Sub);
            var (start, end) = useParen ? _parens : _nothing;
            return $"{start}{_left.AsString(_op)} {_operator[op]} {_right.AsString(_op)}{end}";
        }
    }

    internal class ResultsEqualityComparer : EqualityComparer<Vector256<int>> {
        public override bool Equals(Vector256<int> x, Vector256<int> y) {
            if (Avx2.IsSupported) return Avx2.MoveMask(Avx2.CompareEqual(x, y).AsByte()) == -1;
            for (int i = 0; i < 6; ++i) {
                if (x.GetElement(i) != y.GetElement(i)) return false;
            }

            return true;
        }

        public override int GetHashCode(Vector256<int> res) {
            return HashCode.Combine(res.GetElement(0), res.GetElement(1), res.GetElement(2),
                res.GetElement(3), res.GetElement(4), res.GetElement(5));
        }
    }

    public static class Solver {
        private static readonly Op[] _operations = { Op.Add, Op.Sub, Op.Mul, Op.Div };
        private static int Comparer(Result a, Result b) => a.Total.CompareTo(b.Total);
        public static int Combinations { get; private set; }
        public static List<Result> Results { get; } = new List<Result>();

        private static readonly HashSet<Vector256<int>> _cache =
            new HashSet<Vector256<int>>(130_000, new ResultsEqualityComparer());

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

        public static void Solve(List<long> numbers, long goal) {
            _cache.Clear();
            Combinations = 0;
            var candidates = new Result[6];
            for (int n = 0; n < numbers.Count; n++)
                candidates[n] = new ValRes(numbers[n]);
            Array.Sort(candidates, Comparer);
            Results.Clear();
            SolveInternal(candidates, goal);
            Results.Sort(Comparer);
        }


        private static Result Combine(Op op, Result x, Result y) {
            Combinations++;
            return new AppRes(op, x, y, Apply(op, x.Total, y.Total));
        }

        private static void SolveInternal(Result[] candidates, long goal) {
            if (candidates.Length <= 1) return;

            int canLen = candidates.Length;
            Span<int> nums = stackalloc int[6];
            for (int c = 0; c < canLen; ++c) {
                nums[c] = (int)candidates[c].Total;
            }

            Vector256<int> vec = Vector256.Create(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5],
                0, 0);
            if (_cache.Contains(vec)) {
                return;
            }

            _cache.Add(vec);

            for (int i = 0; i < canLen; ++i) {
                for (int j = 0; j < canLen; ++j) {
                    if (i == j) continue;
                    var x = candidates[i];
                    var y = candidates[j];
                    for (int k = 0; k < _operations.Length; ++k) {
                        var op = _operations[k];
                        if (!IsValid(op, x.Total, y.Total)) continue;
                        var comb = Combine(op, x, y);
                        if (comb.Total == goal) {
                            Results.Add(comb);
                        } else if (canLen > 2) {
                            var rest = new Result[canLen - 1];
                            bool placed = false;
                            int r = 0;
                            for (int l = 0; l < canLen; ++l) {
                                if (l == i || l == j) continue;
                                var can = candidates[l];
                                if (!placed && can.Total >= comb.Total) {
                                    rest[r++] = comb;
                                    placed = true;
                                }

                                rest[r++] = can;
                            }

                            if (!placed) rest[r] = comb;
                            SolveInternal(rest, goal);
                        }
                    }
                }
            }
        }
    }
}
