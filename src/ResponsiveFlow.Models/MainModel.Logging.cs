using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Output directory: {outputDirectory}")]
    partial void LogOutputDirectory(string outputDirectory);
}
