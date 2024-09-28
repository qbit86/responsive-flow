using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ResponseFuture = System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private const int ResponseChannelCapacity = 32;

    private readonly ILogger _logger;
    private readonly ProjectDto _projectDto;
    private readonly Channel<ResponseFuture> _responseChannel;
    private readonly List<Uri> _uris;

    private ProjectRunner(ProjectDto projectDto,
        List<Uri> uris,
        string outputDirectory,
        Channel<ResponseFuture> responseChannel,
        ILogger logger)
    {
        _projectDto = projectDto;
        _uris = uris;
        OutputDirectory = outputDirectory;
        _responseChannel = responseChannel;
        _logger = logger;
    }

    internal IReadOnlyList<Uri> Uris => _uris;

    internal string OutputDirectory { get; }

    internal ChannelReader<ResponseFuture> ResponseChannelReader => _responseChannel.Reader;

    internal ChannelWriter<ResponseFuture> ResponseChannelWriter => _responseChannel.Writer;

    internal static ProjectRunner Create(ProjectDto projectDto, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var responseChannel = Channel.CreateBounded<ResponseFuture>(ResponseChannelCapacity);
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        return new(projectDto, validUris, effectiveOutputDirectory, responseChannel, logger);
    }

    internal async Task<Report> RunAsync(CancellationToken cancellationToken)
    {
        string[] urls = _projectDto.GetUrlsOrEmpty();
        for (int i = 0; i < urls.Length && !cancellationToken.IsCancellationRequested; ++i)
        {
            string url = urls[i];
            LogProcessUrl(url, i, urls.Length);
            await Task.Delay(1000, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        return new();
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
}
