using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ResponsiveFlow;

public sealed record RequestCollectedData(
    int UriIndex,
    Uri Uri,
    int AttemptIndex,
    long StartingTimestamp,
    long EndingTimestamp,
    Task<HttpResponseMessage> ResponseFuture)
{
    public TimeSpan Duration() => Stopwatch.GetElapsedTime(StartingTimestamp, EndingTimestamp);

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(RequestCollectedData));
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
        builder.Append($", {nameof(AttemptIndex)} = ").Append(AttemptIndex);
        builder.Append($", {nameof(Duration)} = ").Append(Duration()).Append("ms");
        builder.Append($", {nameof(ResponseFuture)}.Status = ").Append(ResponseFuture.Status);
        return true;
    }
}
