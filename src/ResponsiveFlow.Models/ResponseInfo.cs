using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResponsiveFlow;

public sealed record ResponseInfo(
    int UriIndex,
    Uri Uri,
    int AttemptIndex,
    long StartingTimestamp,
    Task<long> EndingTimestampFuture,
    Task<HttpResponseMessage> ResponseFuture);
