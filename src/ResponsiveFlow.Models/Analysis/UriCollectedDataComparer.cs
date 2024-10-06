using System.Collections.Generic;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;

namespace ResponsiveFlow;

internal sealed class UriCollectedDataComparer : IComparer<UriCollectedData>
{
    private readonly SimpleEquivalenceTest _test;

    internal UriCollectedDataComparer(SimpleEquivalenceTest test) => _test = test;

    internal static UriCollectedDataComparer Instance { get; } = new(new(MannWhitneyTest.Instance));

    public int Compare(UriCollectedData? x, UriCollectedData? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (y is null)
            return -1;
        if (x is null)
            return 1;

        int heuristicComparison = MetricsHeuristicComparer.Instance.Compare(x.Metrics, y.Metrics);
        if (heuristicComparison != 0)
            return heuristicComparison;

        var sampleX = x.Sample!;
        var sampleY = y.Sample!;
        var comparisonResult = _test.Perform(sampleX, sampleY, Threshold.Zero, SignificanceLevel.P05);
        return comparisonResult switch
        {
            ComparisonResult.Lesser => -1,
            ComparisonResult.Greater => 1,
            _ => x.Metrics!.Median.CompareTo(y.Metrics!.Median)
        };
    }
}
