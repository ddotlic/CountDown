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

    internal class ResultsEqualityComparer : EqualityComparer<Vector128<int>> {
        private readonly Vector128<int> _allBitsSet = Vector128.Create(-1, -1, -1, -1);

        public override bool Equals(Vector128<int> x, Vector128<int> y) {
            if(Sse41.IsSupported) Sse41.TestZ(Sse2.Xor(x, y), _allBitsSet);
            for (int i = 0; i < 4; ++i) {
                if (x.GetElement(i) != y.GetElement(i)) return false;
            }

            return true;
        }

        public override int GetHashCode(Vector128<int> v) {
            return HashCode.Combine(v.GetElement(0), v.GetElement(1), v.GetElement(2), v.GetElement(3));
        }
    }

    public static class Solver {
        private const int _usedBits = 21;
        private static readonly Op[] _operations = { Op.Add, Op.Sub, Op.Mul, Op.Div };
        private static int Comparer(Result a, Result b) => a.Total.CompareTo(b.Total);
        public static int Combinations { get; private set; }
        public static List<Result> Results { get; } = new List<Result>();

        private static readonly HashSet<Vector128<int>> _cache =
            new HashSet<Vector128<int>>(130_000, new ResultsEqualityComparer());

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
            Span<int> nums = stackalloc int[4];
            for (int c = 0; c < canLen; ++c) {
                int shift = _usedBits * (c % 3);
                int ix = c < 3 ? 0 : 2;
                long bits = candidates[c].Total << shift;
                nums[ix] |= (int)(bits & 0xFFFFFFFF);
                nums[ix + 1] |= (int)(bits >> 32);
            }

            Vector128<int> vec = Vector128.Create(nums[0], nums[1], nums[2], nums[3]);

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
