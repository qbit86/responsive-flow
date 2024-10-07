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
        int uriIndex, Uri uri, ICollection<RequestCollectedData> requestCollectedDataset,
        Sample? sample = null, Metrics? metrics = null)
    {
        UriIndex = uriIndex;
        Uri = uri;
        RequestCollectedDataset = requestCollectedDataset;
        Sample = sample;
        Metrics = metrics;
    }

    public int UriIndex { get; }

    public Uri Uri { get; }

    public ICollection<RequestCollectedData> RequestCollectedDataset { get; }

    public Sample? Sample { get; }

    public Metrics? Metrics { get; }

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
        if (values.Count is 0)
            return new(uriIndex, uri, requestCollectedDataset);

        Sample sample = new(values, TimeUnit.Millisecond);
        var metrics = Metrics.Create(sample);
        return new(uriIndex, uri, requestCollectedDataset, sample, metrics);
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
        if (Metrics is not null)
        {
            builder.Append(", ");
            Metrics.PrintMembers(builder);
        }

        return true;
    }

    public void Deconstruct(out int uriIndex, out Uri uri, out Metrics? metrics)
    {
        uriIndex = UriIndex;
        uri = Uri;
        metrics = Metrics;
    }
}
