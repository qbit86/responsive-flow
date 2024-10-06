using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private static IReadOnlyList<string> BinColors => SvgHistogramColors.Blue;

    private async Task BuildThenSaveHistogramAsync(
        UriCollectedData uriCollectedData, CancellationToken cancellationToken)
    {
        if (uriCollectedData.Sample is not { } sample)
            return;

        var histogramPolicy = HistogramPolicy.Instance;
        var binPolicy = HistogramBinPolicy.Instance;
        var histogram = SimpleHistogramBuilder.Instance.Build(sample.Values);
        var bins = histogramPolicy.GetBins(histogram);
        SvgHistogramBuilder<HistogramBin> builder = new();
        var root = builder.Build(bins, binPolicy);
        XDocument doc = new(root);

        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        (int uriIndex, var uri, _) = uriCollectedData;
        string uriString = uri.GetComponents(
            UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.SafeUnescaped);
        string[] validParts = uriString.Split(Path.GetInvalidFileNameChars(), options);
        string uriSlug = string.Join("-", validParts);
        _ = Directory.CreateDirectory(OutputDirectory);
        string path = Path.Join(OutputDirectory, $"{uriIndex}-{uriSlug}.svg");
        Stream stream = File.OpenWrite(path);
        await using (stream)
        {
            var task = doc.SaveAsync(stream, SaveOptions.None, cancellationToken);
            await task.ConfigureAwait(false);
        }
    }
}
