using System.Collections.Generic;

namespace ResponsiveFlow;

internal sealed class UriCollectedDataComparer : IComparer<UriCollectedData>
{
    internal static UriCollectedDataComparer Instance { get; } = new();

    public int Compare(UriCollectedData? x, UriCollectedData? y)
    {
        var xMetrics = x!.Metrics;
        var yMetrics = y!.Metrics;
        if (ReferenceEquals(xMetrics, yMetrics))
            return 0;
        if (xMetrics is null)
            return 1;
        if (yMetrics is null)
            return -1;

        return xMetrics.Mean.CompareTo(yMetrics.Mean);
    }
}
