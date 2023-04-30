using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Packed = System.ValueTuple<long, long>;

namespace CountDown;

public static class Fmt {
    private static readonly long[] _priority = { 1, 2, 3, 3 };
    private static readonly string[] _operator = { "+", "-", "*", "/" };
    private static readonly (string, string) _parens = ("(", ")");
    private static readonly (string, string) _nothing = (string.Empty, string.Empty);

    public static string Format(this Result res) => res.AsString(Op.Add);

    private static string AsString(this Result res, Op parentOp) {
        switch (res) {
            case AppRes appr:
                var op = appr.Op;
                var opi = (int)op;
                var useParen = _priority[(int)parentOp] > _priority[opi] ||
                               (parentOp == op && op == Op.Sub);
                var (start, end) = useParen ? _parens : _nothing;
                return
                    $"{start}{appr.Left.AsString(op)} {_operator[opi]} {appr.Right.AsString(op)}{end}";

            case ValRes valr:
                return valr.Total.ToString();
            default:
                throw new ArgumentException("Unknown result");
        }
    }
}

public enum Op { Add, Sub, Mul, Div }

public abstract class Result {
    public long Total;

    public abstract int Operations { get; }
}

public class ValRes : Result {
    public ValRes(long val) => Total = val;

    public override int Operations => 0;
}

public class AppRes : Result {
    public readonly Result Left;
    public readonly Op Op;
    public readonly Result Right;

    public AppRes(Op op, Result left, Result right) {
        Op = op;
        Left = left;
        Right = right;
        Apply();
    }

    public override int Operations => 1 + Left.Operations + Right.Operations;

    private void Apply() =>
        Total = Op switch {
            Op.Add => Left.Total + Right.Total,
            Op.Sub => Left.Total - Right.Total,
            Op.Mul => Left.Total * Right.Total,
            Op.Div => Left.Total / Right.Total,
            _ => throw new ArgumentException("Operator not supported")
        };
}

public static class Solver {
    private static readonly Op[] _operations = { Op.Add, Op.Sub, Op.Mul, Op.Div };

    private static readonly HashSet<Packed> _cache = new(130_000);

    private static readonly Result[] _memory = new Result[16];
    private static int _offset;

    private static readonly (int, int)[] _packIndices = { (0, 0), (21, 0), (42, 0), (0, 1), (21, 1), (42, 1) };

    public static int Combinations { get; private set; }
    public static List<Result> Results { get; } = new();
    private static int TotalComparer(Result a, Result b) => a.Total.CompareTo(b.Total);
    private static int OperationsComparer(Result a, Result b) => a.Operations.CompareTo(b.Operations);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValid(Op op, long x, long y) =>
        op switch {
            Op.Add => x <= y,
            Op.Sub => x > y,
            Op.Mul => x != 1 && y != 1 && x <= y,
            Op.Div => (y > 1) & (x % y == 0),
            _ => throw new ArgumentException("Operator not supported")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<Result> GetSlice(int len) {
        var slice = new Span<Result>(_memory, _offset, len);
        _offset += len;
        return slice;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReturnSlice(int len) => _offset -= len;

    public static void Solve(List<long> numbers, long goal, int turns) {
        var candidates = new Result[6];
        for (var n = 0; n < numbers.Count; n++) {
            candidates[n] = new ValRes(numbers[n]);
        }

        Array.Sort(candidates, TotalComparer);

        for (int t = 0; t < turns; t++) {
            _cache.Clear();
            Combinations = 0;
            Results.Clear();
            SolveInternal(candidates, goal);
        }

        Results.Sort(OperationsComparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Result Combine(Op op, Result x, Result y) {
        Combinations++;
        return new AppRes(op, x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Packed PackCandidates(Span<Result> candidates) {
        var canLen = candidates.Length;
        Span<long> nums = stackalloc long[2];
        for (var c = 0; c < canLen; ++c) {
            var (shift, ix) = _packIndices[c];
            nums[ix] |= candidates[c].Total << shift;
        }

        return new Packed(nums[1], nums[0]);
    }

    private static void SolveInternal(Span<Result> candidates, long goal) {
        var packed = PackCandidates(candidates);
        if (_cache.Contains(packed)) {
            return;
        }

        _cache.Add(packed);

        var canLen = candidates.Length;
        for (var i = 0; i < canLen; ++i) {
            for (var j = 0; j < canLen; ++j) {
                if (i == j) {
                    continue;
                }

                var x = candidates[i];
                var y = candidates[j];
                for (var k = 0; k < 4; ++k) {
                    var op = _operations[k];
                    if (!IsValid(op, x.Total, y.Total)) {
                        continue;
                    }

                    var comb = Combine(op, x, y);
                    if (comb.Total == goal) {
                        Results.Add(comb);
                    } else if (canLen > 2) {
                        var rest = GetSlice(canLen - 1);
                        var placed = false;
                        var r = 0;
                        for (var l = 0; l < canLen; ++l) {
                            if (l == i || l == j) {
                                continue;
                            }

                            var can = candidates[l];
                            if (!placed && can.Total >= comb.Total) {
                                rest[r++] = comb;
                                placed = true;
                            }

                            rest[r++] = can;
                        }

                        if (!placed) {
                            rest[r] = comb;
                        }

                        SolveInternal(rest, goal);
                        ReturnSlice(canLen - 1);
                    }
                }
            }
        }
    }
}
