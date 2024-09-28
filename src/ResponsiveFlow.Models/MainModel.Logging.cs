using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    [LoggerMessage(
        EventId = 1, Level = LogLevel.Information, Message = "Processing project '{outputDirectory}'...")]
    partial void LogProcessingProject(string outputDirectory);

    [LoggerMessage(
        EventId = 2, Level = LogLevel.Information, Message = "Processed project '{outputDirectory}'")]
    partial void LogProcessedProject(string outputDirectory);
}
