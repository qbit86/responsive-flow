using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Channel<InAppMessage> _messageChannel;
    private readonly Progress<double> _progress = new();

    public MainModel(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _messageChannel = Channel.CreateUnbounded<InAppMessage>(new UnboundedChannelOptions { SingleReader = true });
        _logger = loggerFactory.CreateLogger<MainModel>();
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public ChannelReader<InAppMessage> MessageChannelReader => _messageChannel.Reader;

    public event EventHandler<double> ProgressChanged
    {
        add => _progress.ProgressChanged += value;
        remove => _progress.ProgressChanged -= value;
    }

    public Task<ProjectReportDto> RunAsync(ProjectDto projectDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectDto);

        return RunUncheckedAsync(projectDto, cancellationToken);
    }

    private async Task<ProjectReportDto> RunUncheckedAsync(ProjectDto projectDto, CancellationToken cancellationToken)
    {
        var projectRunner = ProjectRunner.Create(
            projectDto, _httpClient, _messageChannel.Writer, _progress, _loggerFactory);
        LogProcessingProject(projectRunner.OutputDirectory);
        try
        {
            var collectedDataFuture = projectRunner.RunAsync(cancellationToken);
            var projectCollectedData = await collectedDataFuture.ConfigureAwait(false);

            await WriteRankingAsync(projectCollectedData.UriCollectedDataset, cancellationToken)
                .ConfigureAwait(false);

            var projectReport = ProjectReportDto.Create(projectCollectedData);
            _ = Directory.CreateDirectory(projectRunner.OutputDirectory);
            string path = Path.Join(projectRunner.OutputDirectory, "report.json");
            Stream utf8Json = File.OpenWrite(path);
            await using (utf8Json)
            {
                var serializerTask = JsonSerializer.SerializeAsync(utf8Json, projectReport,
                    ProjectReportJsonSerializerContext.Default.ProjectReportDto, cancellationToken);
                await serializerTask.ConfigureAwait(false);
                var message = InAppMessage.FromMessage($"Saved report to '{path}'", LogLevel.Debug);
                await _messageChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }

            return projectReport;
        }
        finally
        {
            LogProcessedProject(projectRunner.OutputDirectory);
        }
    }

    private async Task WriteRankingAsync(
        IReadOnlyList<UriCollectedData> uriCollectedDataset, CancellationToken cancellationToken)
    {
        if (uriCollectedDataset.Count is 0)
            return;

        StringBuilder builder = new(uriCollectedDataset.Count * 64);
        builder.AppendLine("Ranking of URLs by response time:");
        for (int i = 0; i < uriCollectedDataset.Count; ++i)
        {
            if (i > 0)
                builder.AppendLine();
            builder.Append(i).Append(".\t#");
            var uriCollectedData = uriCollectedDataset[i];
            builder.Append(uriCollectedData.UriIndex);
            builder.Append('\t').Append(uriCollectedData.Uri);
        }

        var message = InAppMessage.FromMessage(builder.ToString(), LogLevel.Debug);
        await _messageChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
