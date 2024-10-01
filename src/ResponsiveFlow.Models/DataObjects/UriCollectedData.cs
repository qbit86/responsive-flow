using System;
using System.Collections.Generic;
using System.Text;

namespace ResponsiveFlow;

public sealed record UriCollectedData(
    int UriIndex,
    Uri Uri,
    ICollection<RequestCollectedData> RequestCollectedDataset)
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
        builder.Append($"{nameof(UriIndex)} = ").Append(UriIndex);
        builder.Append($", {nameof(Uri)} = ").Append(Uri);
        builder.Append($", {nameof(RequestCollectedDataset)}.Count = ").Append(RequestCollectedDataset.Count);
        return true;
    }
}
