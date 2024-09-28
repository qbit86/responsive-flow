using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private readonly ILogger _logger;
    private readonly ProjectDto _projectDto;

    private ProjectRunner(ProjectDto projectDto, string effectiveOutputDirectory, ILogger logger)
    {
        _projectDto = projectDto;
        OutputDirectory = effectiveOutputDirectory;
        _logger = logger;
    }

    internal string OutputDirectory { get; }

    internal static ProjectRunner Create(ProjectDto projectDto, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        return new(projectDto, effectiveOutputDirectory, logger);
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
        if (projectDto.TryGetOutputDir(out string? outputDirectory))
            return outputDirectory;

        string basename = $"{startTime:dd_HH-mm-ss}";
        return Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(ResponsiveFlow), basename);
    }
}
