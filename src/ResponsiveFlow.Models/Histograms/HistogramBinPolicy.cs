using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed class HistogramBinPolicy : IBinPolicy<HistogramBin>
{
    internal static HistogramBinPolicy Instance { get; } = new();

    public double Lower(HistogramBin bin) => bin.Lower;

    public double Upper(HistogramBin bin) => bin.Upper;

    public double Height(HistogramBin bin) => bin.Count;
}
