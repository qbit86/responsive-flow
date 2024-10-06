using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Perfolizer.Mathematics.Histograms;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    /// <seealso href="https://en.wikipedia.org/wiki/Web_colors#Extended_colors" />
    private static string[] BinColors { get; } = ["LightSkyBlue", "SkyBlue"];

    private async Task BuildThenSaveHistogramAsync(
        UriCollectedData uriCollectedData, CancellationToken cancellationToken)
    {
        if (uriCollectedData.Sample is not { } sample)
            return;

        var histogramPolicy = HistogramPolicy.Instance;
        var histogramBinPolicy = HistogramBinPolicy.Instance;
        var histogram = SimpleHistogramBuilder.Instance.Build(sample.Values);
        (int uriIndex, var uri, _) = uriCollectedData;
        var bins = histogramPolicy.GetBins(histogram);
        const double margin = 8.0;
        const double pivotX = margin;
        const double pivotY = margin;

        const double maxRectWidthPx = 512.0;
        const double minRectHeightPx = 24.0;
        double minRectHeightMs = bins.Min(histogramBinPolicy.Gap);
        double maxRectHeightMs = bins.Max(histogramBinPolicy.Gap);
        double maxRectHeightPx = minRectHeightPx * maxRectHeightMs / minRectHeightMs;
        double maxRectWidthUnits = bins.Max(histogramBinPolicy.Height);

        double widthPixelsByUnits = maxRectWidthPx / maxRectWidthUnits;
        double heightPixelsByMs = maxRectHeightPx / maxRectHeightMs;

        XNamespace ns = "http://www.w3.org/2000/svg";
        double plotHeight = bins.Sum(it => heightPixelsByMs * histogramBinPolicy.Gap(it));
        XElement root = new(ns + "svg",
            new XAttribute("version", "2"),
            new XAttribute("width", maxRectWidthPx + 2 * margin),
            new XAttribute("height", plotHeight + 2 * margin));
        XDocument doc = new(root);

        double y = pivotY;
        for (int i = 0; i < bins.Count; ++i)
        {
            var bin = bins[i];
            double width = widthPixelsByUnits * histogramBinPolicy.Height(bin);
            double height = heightPixelsByMs * histogramBinPolicy.Gap(bin);

            XElement rect = new(ns + "rect",
                new XAttribute("width", width),
                new XAttribute("height", height),
                new XAttribute("x", pivotX),
                new XAttribute("y", y),
                new XAttribute("fill", BinColors[i & 1]));
            root.Add(rect);

            y += height;
        }

        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        string uriSlug = uri.GetComponents(
            UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.SafeUnescaped);
        string[] validParts = uriSlug.Split(Path.GetInvalidFileNameChars(), options);
        string filename = string.Join("-", validParts);
        _ = Directory.CreateDirectory(OutputDirectory);
        string path = Path.Join(OutputDirectory, $"{uriIndex}-{filename}.svg");
        Stream stream = File.OpenWrite(path);
        await using (stream)
        {
            var task = doc.SaveAsync(stream, SaveOptions.None, cancellationToken);
            await task.ConfigureAwait(false);
        }
    }
}
