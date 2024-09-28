using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Processing '{url}' ({index}/{count})...")]
    partial void LogProcessUrl(string url, int index, int count);
}
