using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Processing URL '{url}' ({index}/{count})...")]
    partial void LogProcessingUrl(string url, int index, int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Processed URL '{url}' ({index}/{count})")]
    partial void LogProcessedUrl(string url, int index, int count);
}
