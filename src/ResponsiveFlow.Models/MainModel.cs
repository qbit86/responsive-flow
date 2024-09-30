using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly ILogger _logger;
    private readonly Channel<InAppMessage> _messageChannel;
    private readonly ProjectRunner _projectRunner;

    public MainModel(IOptions<ProjectDto> projectDto, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _messageChannel = Channel.CreateUnbounded<InAppMessage>(new UnboundedChannelOptions { SingleReader = true });
        _projectRunner = ProjectRunner.Create(projectDto.Value, httpClient, _messageChannel.Writer, loggerFactory);
        _logger = loggerFactory.CreateLogger<MainModel>();
    }

    private static CultureInfo P => CultureInfo.InvariantCulture;

    public ChannelReader<InAppMessage> MessageChannelReader => _messageChannel.Reader;

    public async Task<ProjectReportDto> RunAsync(CancellationToken cancellationToken)
    {
        LogProcessingProject(_projectRunner.OutputDirectory);
        var collectedDataFuture = _projectRunner.RunAsync(cancellationToken);
        try
        {
            var projectCollectedData = await collectedDataFuture.ConfigureAwait(false);
            var projectReport = ProjectReportDto.Create(projectCollectedData);
            StringBuilder builder = new();
            foreach (var uriReport in projectReport.UriReports)
            {
                builder.Clear();
                builder.Append(P, $"#{uriReport.UriIndex} {uriReport.Uri}");
                if (uriReport.Statistics is { } s)
                    _ = s.PrintMembers(builder.AppendLine());

                var level = uriReport.Statistics is null ? LogLevel.Warning : LogLevel.Information;
                var task = _messageChannel.Writer.WriteAsync(
                    InAppMessage.FromMessage(builder.ToString(), level), cancellationToken);
                await task.ConfigureAwait(false);
            }

            _ = Directory.CreateDirectory(_projectRunner.OutputDirectory);
            string path = Path.Join(_projectRunner.OutputDirectory, "report.json");
            Stream fileStream = File.OpenWrite(path);
            await using (fileStream)
            {
                var serializerTask = JsonSerializer.SerializeAsync(fileStream, projectReport,
                    ProjectReportJsonSerializerContext.Default.ProjectReportDto, cancellationToken);
                await serializerTask.ConfigureAwait(false);
                var task = _messageChannel.Writer.WriteAsync(
                    InAppMessage.FromMessage($"Saved report to '{path}'", LogLevel.Debug), cancellationToken);
                await task.ConfigureAwait(false);
            }

            return projectReport;
        }
        finally
        {
            LogProcessedProject(_projectRunner.OutputDirectory);
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ProjectReportDto))]
internal sealed partial class ProjectReportJsonSerializerContext : JsonSerializerContext;
