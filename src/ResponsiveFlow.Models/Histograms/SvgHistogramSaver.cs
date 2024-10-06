using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ResponsiveFlow;

internal sealed class SvgHistogramSaver
{
    internal SvgHistogramSaver(int uriIndex, string uriSlug, string outputDirectory)
    {
        UriIndex = uriIndex;
        OutputDirectory = outputDirectory;
        UriSlug = uriSlug;
    }

    private int UriIndex { get; }

    private string UriSlug { get; }

    private string OutputDirectory { get; }

    internal async Task SaveAsync(
        XElement svgElement,
        string prefix,
        CancellationToken cancellationToken)
    {
        XDocument doc = new(svgElement);

        StringBuilder filenameBuilder = new();
        filenameBuilder.Append(UriIndex);
        if (!string.IsNullOrWhiteSpace(prefix))
            filenameBuilder.Append('-').Append(prefix);
        filenameBuilder.Append('-').Append(UriSlug).Append(".svg");
        string path = Path.Join(OutputDirectory, filenameBuilder.ToString());
        Stream stream = File.OpenWrite(path);
        await using (stream)
        {
            var task = doc.SaveAsync(stream, SaveOptions.None, cancellationToken);
            await task.ConfigureAwait(false);
        }
    }
}
