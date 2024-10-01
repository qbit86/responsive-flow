using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ResponsiveFlow;

public sealed class UriReportDto
{
    private UriReportDto(int uriIndex, Uri uri, Statistics? statistics)
    {
        UriIndex = uriIndex;
        Uri = uri;
        Statistics = statistics;
    }

    public int UriIndex { get; }

    public Uri Uri { get; }

    public Statistics? Statistics { get; }

    public static UriReportDto Create(UriCollectedData uriCollectedData)
    {
        ArgumentNullException.ThrowIfNull(uriCollectedData);

        (int uriIndex, var uri, var requests) = uriCollectedData;
        var values = requests
            .Where(it => it.ResponseFuture.IsCompletedSuccessfully)
            .Select(it => Stopwatch.GetElapsedTime(it.StartingTimestamp, it.EndingTimestamp).TotalMilliseconds)
            .ToList();
        if (values.Count is 0)
            return new(uriIndex, uri, null);
        var statistics = Statistics.Create(values);
        return new(uriIndex, uri, statistics);
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
        builder.Append("UriIndex = ").Append(UriIndex);
        builder.Append(", Uri = ").Append(Uri);
        if (Statistics is { } statistics)
            builder.Append(", Statistics = ").Append(statistics);
        return true;
    }
}
