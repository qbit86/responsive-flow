using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ChannelWriter<InAppMessage> _messageChannelWriter;
    private readonly IProgress<double> _progress;
    private readonly List<Uri> _uris;

    private ProjectRunner(
        List<Uri> uris,
        string outputDirectory,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<double> progress,
        ILogger logger)
    {
        _uris = uris;
        OutputDirectory = outputDirectory;
        _httpClient = httpClient;
        _messageChannelWriter = messageChannelWriter;
        _progress = progress;
        _logger = logger;
    }

    private static CultureInfo P => CultureInfo.InvariantCulture;

    internal string OutputDirectory { get; }

    internal static ProjectRunner Create(
        ProjectDto projectDto,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<double> progress,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageChannelWriter);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        return new(validUris, effectiveOutputDirectory, httpClient, messageChannelWriter, progress, logger);
    }

    internal Task<ProjectCollectedData> RunAsync(CancellationToken cancellationToken)
    {
        if (_uris is [])
            return Task.FromResult(new ProjectCollectedData([]));

        return RunUncheckedAsync(cancellationToken);
    }

    private async Task<ProjectCollectedData> RunUncheckedAsync(CancellationToken cancellationToken)
    {
        var uriCollectedDataset = new UriCollectedData[_uris.Count];
        int attemptCount = 0;
        double totalAttemptCount = _uris.Count * UriRunner.AttemptCount;
        Progress<UriProgressReport> progress = new(HandleProgressChanged);
        for (int uriIndex = 0; !cancellationToken.IsCancellationRequested && uriIndex < _uris.Count; ++uriIndex)
        {
            var uri = _uris[uriIndex];
            LogProcessingUrl(uri, uriIndex, _uris.Count);
            try
            {
                var uriRunner = UriRunner.Create(uriIndex, uri, _httpClient, progress);
                var uriCollectedDataFuture = uriRunner.RunAsync(cancellationToken);
                var uriCollectedData = await uriCollectedDataFuture.ConfigureAwait(false);
                var histogramTask = BuildThenSaveHistogramAsync(uriCollectedData, cancellationToken);
                await histogramTask.ConfigureAwait(false);
                await WriteUriCollectedDataAsync(uriCollectedData, cancellationToken).ConfigureAwait(false);
                uriCollectedDataset[uriIndex] = uriCollectedData;
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                var message = InAppMessage.FromException(exception);
                var task = _messageChannelWriter.WriteAsync(message, cancellationToken);
                await task.ConfigureAwait(false);
            }
            finally
            {
                LogProcessedUrl(uri, uriIndex, _uris.Count);
            }
        }

        return new(uriCollectedDataset);

        void HandleProgressChanged(UriProgressReport report)
        {
            if (report.Exception is { } exception)
            {
                _ = _messageChannelWriter.TryWrite(InAppMessage.FromException(exception));
                return;
            }

            _ = Interlocked.Increment(ref attemptCount);
            double progressValue = attemptCount / totalAttemptCount;
            _progress.Report(progressValue);
        }
    }

    private static string GetOutputDirectoryOrFallback(ProjectDto projectDto, DateTime startTime)
    {
        string basename = $"{startTime.DayOfYear}_{startTime:HH-mm-ss}";
        if (projectDto.TryGetOutputDir(out string? outputRoot))
            return Path.Join(outputRoot, basename);
        return Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(ResponsiveFlow), basename);
    }

    private static List<Uri> GetValidUris(ProjectDto projectDto)
    {
        string[] urlStrings = projectDto.GetUrlsOrEmpty();
        List<Uri> uris = new(urlStrings.Length);
        foreach (string urlString in urlStrings)
        {
            if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
                uris.Add(uri);
        }

        return uris;
    }

    private async Task WriteUriCollectedDataAsync(
        UriCollectedData uriCollectedData, CancellationToken cancellationToken)
    {
        (int uriIndex, var uri, var metrics) = uriCollectedData;
        StringBuilder builder = new();
        builder.Append(P, $"#{uriIndex} {uri}");
        if (metrics is not null)
            _ = metrics.PrintMembers(builder.AppendLine());
        var level = metrics is null ? LogLevel.Warning : LogLevel.Information;
        var task = _messageChannelWriter.WriteAsync(
            InAppMessage.FromMessage(builder.ToString(), level), cancellationToken);
        await task.ConfigureAwait(false);
    }
}
