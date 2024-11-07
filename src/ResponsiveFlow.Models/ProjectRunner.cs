using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly int _maxConcurrentRequests;
    private readonly ChannelWriter<InAppMessage> _messageChannelWriter;
    private readonly IProgress<double> _progress;
    private readonly Lazy<ILogger> _uriRunnerLogger;
    private readonly List<Uri> _uris;

    private ProjectRunner(
        List<Uri> uris,
        string outputDirectory,
        HttpClient httpClient,
        int maxConcurrentRequests,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<double> progress,
        ILogger logger,
        Lazy<ILogger> uriRunnerLogger)
    {
        _uris = uris;
        OutputDirectory = outputDirectory;
        _httpClient = httpClient;
        _maxConcurrentRequests = maxConcurrentRequests;
        _messageChannelWriter = messageChannelWriter;
        _progress = progress;
        _logger = logger;
        _uriRunnerLogger = uriRunnerLogger;
    }

    private static CultureInfo P => CultureInfo.InvariantCulture;

    internal string OutputDirectory { get; }

    internal static ProjectRunner Create(
        ProjectDto projectDto,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<double> progress,
        IConfiguration config,
        ILoggerFactory loggerFactory)
    {
        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        Lazy<ILogger> uriRunnerLogger = new(loggerFactory.CreateLogger<UriRunner>);
        int maxConcurrentRequests = GetMaxConcurrentRequests();
        WriteMaxConcurrentRequests();
        return new(
            validUris, effectiveOutputDirectory, httpClient, maxConcurrentRequests,
            messageChannelWriter, progress, logger, uriRunnerLogger);

        int GetMaxConcurrentRequests(int defaultMaxConcurrentRequests = 20)
        {
            if (config["MaxConcurrentRequests"] is not { Length: > 0 } s)
                return defaultMaxConcurrentRequests;
            if (!int.TryParse(s, out int rawMaxConcurrentRequests))
                return defaultMaxConcurrentRequests;
            return int.Clamp(rawMaxConcurrentRequests, 1, sbyte.MaxValue);
        }

        void WriteMaxConcurrentRequests()
        {
            LogMaxConcurrentRequests(logger, maxConcurrentRequests);
            var message = InAppMessage.FromMessage($"MaxConcurrentRequests: {maxConcurrentRequests}", LogLevel.Debug);
            _ = messageChannelWriter.TryWrite(message);
        }
    }

    internal Task<ProjectCollectedData> RunAsync(CancellationToken cancellationToken) =>
        _uris is [] ? Task.FromResult(new ProjectCollectedData([], [])) : RunUncheckedAsync(cancellationToken);

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
                UriRunner uriRunner = new(
                    uriIndex, uri, _httpClient, _maxConcurrentRequests, progress, _uriRunnerLogger.Value);
                var uriCollectedDataFuture = uriRunner.RunAsync(cancellationToken);
                var uriCollectedData = await uriCollectedDataFuture.ConfigureAwait(false);
                await BuildThenSaveHistogramsAsync(uriCollectedData, cancellationToken).ConfigureAwait(false);
                await WriteUriCollectedDataAsync(uriCollectedData, cancellationToken).ConfigureAwait(false);
                uriCollectedDataset[uriIndex] = uriCollectedData;
            }
            catch (Exception exception)
            {
                LogException(exception);
                var message = InAppMessage.FromException(exception);
                await _messageChannelWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                LogProcessedUrl(uri, uriIndex, _uris.Count);
            }
        }

        uriCollectedDataset.AsSpan().Sort(UriCollectedDataComparer.Instance);
        int[] ranks = RankHelpers.GetRanksOrdered(uriCollectedDataset, EquivalenceComparer.Instance);
        return new(uriCollectedDataset, ranks);

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
        var message = InAppMessage.FromMessage(builder.ToString(), level);
        await _messageChannelWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }
}

file sealed class EquivalenceComparer : IEqualityComparer<UriCollectedData>
{
    internal static EquivalenceComparer Instance { get; } = new();

    public bool Equals(UriCollectedData? x, UriCollectedData? y) =>
        SampleEquivalenceComparer.Default.Equals(x?.Sample, y?.Sample);

    public int GetHashCode(UriCollectedData obj) => throw new NotSupportedException();
}
