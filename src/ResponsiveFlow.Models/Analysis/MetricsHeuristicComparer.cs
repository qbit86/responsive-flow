using System.Collections.Generic;

namespace ResponsiveFlow;

internal sealed class MetricsHeuristicComparer : IComparer<Metrics>
{
    internal static MetricsHeuristicComparer Instance { get; } = new();

    public int Compare(Metrics? x, Metrics? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (y is null)
            return -1;
        if (x is null)
            return 1;

        if (IsLessByRange(x, y))
            return -1;
        if (IsLessByRange(y, x))
            return 1;

        if (IsLessByTukey(x, y))
            return -1;
        if (IsLessByTukey(y, x))
            return 1;

        if (IsLessByThreeSigma(x, y))
            return -1;
        if (IsLessByThreeSigma(y, x))
            return 1;

        return 0;
    }

    private static bool IsLessByRange(Metrics x, Metrics y) => x.Max < y.Min;

    private static bool IsLessByTukey(Metrics x, Metrics y) =>
        x.Quartiles.Q3 + 1.5 * x.InterquartileRange < y.Quartiles.Q1 - 1.5 * y.InterquartileRange;

    private static bool IsLessByThreeSigma(Metrics x, Metrics y) =>
        x.Mean + 3.0 * x.StandardDeviation < y.Mean - 3.0 * y.StandardDeviation;
}
