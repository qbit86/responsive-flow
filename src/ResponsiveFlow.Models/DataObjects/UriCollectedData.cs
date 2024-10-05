using System;
using System.Collections.Generic;
using System.Text;

namespace ResponsiveFlow;

public sealed record UriCollectedData
{
    private UriCollectedData(int uriIndex, Uri uri, ICollection<RequestCollectedData> requestCollectedDataset)
    {
        UriIndex = uriIndex;
        Uri = uri;
        RequestCollectedDataset = requestCollectedDataset;
    }

    public int UriIndex { get; }

    public Uri Uri { get; }

    public ICollection<RequestCollectedData> RequestCollectedDataset { get; }

    internal static UriCollectedData Create(
        int uriIndex, Uri uri, ICollection<RequestCollectedData> requestCollectedDataset)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(requestCollectedDataset);

        return new(uriIndex, uri, requestCollectedDataset);
    }

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

    public void Deconstruct(
        out int uriIndex, out Uri uri, out ICollection<RequestCollectedData> requestCollectedDataset)
    {
        uriIndex = UriIndex;
        uri = Uri;
        requestCollectedDataset = RequestCollectedDataset;
    }
}
