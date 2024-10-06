using System.Collections.Generic;
using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed class HistogramPolicy : IHistogramPolicy<HistogramBin, Histogram>
{
    internal static HistogramPolicy Instance { get; } = new();

    public IReadOnlyList<HistogramBin> GetBins(Histogram histogram) => histogram.Bins;
}
