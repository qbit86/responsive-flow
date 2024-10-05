using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perfolizer;
using Perfolizer.Horology;

namespace ResponsiveFlow;

public sealed record UriCollectedData
{
    private UriCollectedData(
        int uriIndex, Uri uri, ICollection<RequestCollectedData> requestCollectedDataset, Sample? sample)
    {
        UriIndex = uriIndex;
        Uri = uri;
        RequestCollectedDataset = requestCollectedDataset;
        Sample = sample;
    }

    public int UriIndex { get; }

    public Uri Uri { get; }

    public ICollection<RequestCollectedData> RequestCollectedDataset { get; }

    public Sample? Sample { get; }

    public int SampleSize => (Sample?.Size).GetValueOrDefault();

    internal static UriCollectedData Create(
        int uriIndex, Uri uri, ICollection<RequestCollectedData> requestCollectedDataset)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(requestCollectedDataset);

        var values = requestCollectedDataset
            .Where(it => it.ResponseFuture.IsCompletedSuccessfully)
            .Select(it => it.Duration().TotalMilliseconds)
            .ToList();
        Sample? sample = values.Count > 0 ? new(values, TimeUnit.Millisecond) : null;
        return new(uriIndex, uri, requestCollectedDataset, sample);
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
        int sampleSize = SampleSize;
        builder.Append($", {nameof(SampleSize)} = ").Append(sampleSize);
        if (RequestCollectedDataset.Count != sampleSize)
            builder.Append($", {nameof(RequestCollectedDataset)}.Count = ").Append(RequestCollectedDataset.Count);
        return true;
    }

    public void Deconstruct(out int uriIndex, out Uri uri, out Sample? sample)
    {
        uriIndex = UriIndex;
        uri = Uri;
        sample = Sample;
    }
}
