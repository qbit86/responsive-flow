using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed class DensityHistogramBinPolicy : IBinPolicy<DensityHistogramBin>
{
    internal static DensityHistogramBinPolicy Instance { get; } = new();

    public double Lower(DensityHistogramBin bin) => bin.Lower;

    public double Upper(DensityHistogramBin bin) => bin.Upper;

    public double Height(DensityHistogramBin bin) => bin.Height;
}
