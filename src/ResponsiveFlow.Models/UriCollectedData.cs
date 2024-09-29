using System;
using System.Collections.Concurrent;
using System.Text;

namespace ResponsiveFlow;

public sealed record UriCollectedData(
    int UriIndex,
    Uri Uri,
    ConcurrentBag<RequestCollectedData> Requests)
{
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(UriCollectedData));
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
        builder.Append(", Requests.Count = ").Append(Requests.Count);
        return true;
    }
}
