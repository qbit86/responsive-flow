using System.Collections.Generic;
using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed class DensityHistogramPolicy : IHistogramPolicy<DensityHistogramBin, DensityHistogram>
{
    internal static DensityHistogramPolicy Instance { get; } = new();

    public IReadOnlyList<DensityHistogramBin> GetBins(DensityHistogram histogram) => histogram.Bins;
}
