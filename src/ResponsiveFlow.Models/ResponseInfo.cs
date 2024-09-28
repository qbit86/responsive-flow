using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResponsiveFlow;

public readonly record struct ResponseInfo(
    int UriIndex,
    Uri Uri,
    int AttemptIndex,
    Task<HttpResponseMessage> Future);
