using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Channel<InAppMessage> _messageChannel;
    private readonly Progress<double> _progress = new();
    private readonly ProjectDto _projectDto;

    public MainModel(IOptions<ProjectDto> projectDto, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _messageChannel = Channel.CreateUnbounded<InAppMessage>(new UnboundedChannelOptions { SingleReader = true });
        _logger = loggerFactory.CreateLogger<MainModel>();
        _projectDto = projectDto.Value;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public ChannelReader<InAppMessage> MessageChannelReader => _messageChannel.Reader;

    public event EventHandler<double> ProgressChanged
    {
        add => _progress.ProgressChanged += value;
        remove => _progress.ProgressChanged -= value;
    }

    public async Task<ProjectReportDto> RunAsync(CancellationToken cancellationToken)
    {
        var projectRunner = ProjectRunner.Create(
            _projectDto, _httpClient, _messageChannel.Writer, _progress, _loggerFactory);
        LogProcessingProject(projectRunner.OutputDirectory);
        try
        {
            var collectedDataFuture = projectRunner.RunAsync(cancellationToken);
            var projectCollectedData = await collectedDataFuture.ConfigureAwait(false);
            var projectReport = ProjectReportDto.Create(projectCollectedData);
            _ = Directory.CreateDirectory(projectRunner.OutputDirectory);
            string path = Path.Join(projectRunner.OutputDirectory, "report.json");
            Stream utf8Json = File.OpenWrite(path);
            await using (utf8Json)
            {
                var serializerTask = JsonSerializer.SerializeAsync(utf8Json, projectReport,
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
            LogProcessedProject(projectRunner.OutputDirectory);
        }
    }
}
