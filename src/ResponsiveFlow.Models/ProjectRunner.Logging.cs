using System;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Processing URL '{uri}' ({index}/{count})...")]
    partial void LogProcessingUrl(Uri uri, int index, int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Processed URL '{uri}' ({index}/{count})")]
    partial void LogProcessedUrl(Uri uri, int index, int count);
}
