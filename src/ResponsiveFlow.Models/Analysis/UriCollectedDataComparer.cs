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
        var xSample = x?.Sample;
        var ySample = y?.Sample;
        if (ReferenceEquals(xSample, ySample))
            return 0;
        if (ySample is null)
            return -1;
        if (xSample is null)
            return 1;

        var xMetrics = x!.Metrics;
        var yMetrics = y!.Metrics;
        int heuristicComparison = MetricsHeuristicComparer.Instance.Compare(xMetrics, yMetrics);
        if (heuristicComparison != 0)
            return heuristicComparison;

        var comparisonResult = _test.Perform(xSample, ySample, Threshold.Zero, SignificanceLevel.P05);
        return comparisonResult switch
        {
            ComparisonResult.Lesser => -1,
            ComparisonResult.Greater => 1,
            _ => ConservativeEstimate(xMetrics!).CompareTo(ConservativeEstimate(yMetrics!))
        };
    }

    private static double ConservativeEstimate(Metrics metrics) => metrics.Mean + 3.0 * metrics.StandardDeviation;
}
