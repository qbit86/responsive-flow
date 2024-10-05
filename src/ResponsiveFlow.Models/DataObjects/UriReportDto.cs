using System;
using System.Text;

namespace ResponsiveFlow;

public sealed class UriReportDto
{
    private UriReportDto(int uriIndex, Uri uri, Metrics? metrics)
    {
        UriIndex = uriIndex;
        Uri = uri;
        Metrics = metrics;
    }

    public int UriIndex { get; }

    public Uri Uri { get; }

    public Metrics? Metrics { get; }

    public static UriReportDto Create(UriCollectedData uriCollectedData)
    {
        ArgumentNullException.ThrowIfNull(uriCollectedData);

        (int uriIndex, var uri, var sample) = uriCollectedData;
        if (sample is not { Size: > 0 })
            return new(uriIndex, uri, null);
        var metrics = Metrics.Create(sample);
        return new(uriIndex, uri, metrics);
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(UriReportDto));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(UriIndex)} = ").Append(UriIndex);
        builder.Append($", {nameof(Uri)} = ").Append(Uri);
        if (Metrics is { } metrics)
            builder.Append($", {nameof(Metrics)} = ").Append(metrics);
        return true;
    }
}
