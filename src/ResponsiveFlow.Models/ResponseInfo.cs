using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ResponsiveFlow;

public sealed record ResponseInfo(
    int UriIndex,
    Uri Uri,
    int AttemptIndex,
    long StartingTimestamp,
    Task<long> EndingTimestampFuture,
    Task<HttpResponseMessage> ResponseFuture)
{
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(ResponseInfo));
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
        if (EndingTimestampFuture.IsCompletedSuccessfully)
        {
            var elapsedTime = Stopwatch.GetElapsedTime(StartingTimestamp, EndingTimestampFuture.Result);
            builder.Append(", ElapsedTime = ").Append(elapsedTime.TotalMilliseconds).Append("ms");
        }
        else
        {
            builder.Append(", StartingTimestamp = ").Append(StartingTimestamp);
        }

        builder.Append(", ResponseFuture.Status = ").Append(ResponseFuture.Status);
        return true;
    }
}
