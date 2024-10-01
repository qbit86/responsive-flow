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
        builder.Append("UriIndex = ").Append(UriIndex);
        builder.Append(", Uri = ").Append(Uri);
        builder.Append(", AttemptIndex = ").Append(AttemptIndex);
        var elapsedTime = Stopwatch.GetElapsedTime(StartingTimestamp, EndingTimestamp);
        builder.Append(", ElapsedTime = ").Append(elapsedTime.TotalMilliseconds).Append("ms");
        builder.Append(", ResponseFuture.Status = ").Append(ResponseFuture.Status);
        return true;
    }
}
