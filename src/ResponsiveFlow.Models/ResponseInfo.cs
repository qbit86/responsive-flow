using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResponsiveFlow;

public readonly record struct ResponseInfo(
    int Index,
    Uri Uri,
    Task<HttpResponseMessage> Future);
