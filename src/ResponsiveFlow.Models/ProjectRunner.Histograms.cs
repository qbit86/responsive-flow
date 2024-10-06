using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private static IReadOnlyList<string> BinColors => SvgHistogramColors.Blue;

    private async Task BuildThenSaveHistogramsAsync(
        UriCollectedData uriCollectedData, CancellationToken cancellationToken)
    {
        if (uriCollectedData.Sample is not { } sample)
            return;

        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        (int uriIndex, var uri, _) = uriCollectedData;
        string uriString = uri.GetComponents(
            UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.SafeUnescaped);
        string[] validParts = uriString.Split(Path.GetInvalidFileNameChars(), options);
        string uriSlug = string.Join("-", validParts);
        _ = Directory.CreateDirectory(OutputDirectory);
        SvgHistogramSaver saver = new(uriIndex, uriSlug, OutputDirectory);

        {
            var histogram = SimpleHistogramBuilder.Instance.Build(sample.Values);
            var histogramPolicy = HistogramPolicy.Instance;
            var binPolicy = HistogramBinPolicy.Instance;
            var bins = histogramPolicy.GetBins(histogram);
            SvgHistogramBuilder<HistogramBin> builder = new() { Colors = SvgHistogramColors.Blue };
            var svgElement = builder.Build(bins, binPolicy);
            await saver.SaveAsync(svgElement, "histogram", cancellationToken).ConfigureAwait(false);
        }

        {
            var histogram = QuantileRespectfulDensityHistogramBuilder.Instance.Build(sample.Values, 4);
            var histogramPolicy = DensityHistogramPolicy.Instance;
            var binPolicy = DensityHistogramBinPolicy.Instance;
            var bins = histogramPolicy.GetBins(histogram);
            SvgHistogramBuilder<DensityHistogramBin> builder = new() { Colors = SvgHistogramColors.Green };
            var svgElement = builder.Build(bins, binPolicy);
            await saver.SaveAsync(svgElement, "quartiles", cancellationToken).ConfigureAwait(false);
        }
    }
}
