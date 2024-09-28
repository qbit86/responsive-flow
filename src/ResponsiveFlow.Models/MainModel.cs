using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly ILogger _logger;
    private readonly ProjectRunner _projectRunner;

    public MainModel(IOptions<ProjectDto> projectDto, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _projectRunner = ProjectRunner.Create(projectDto.Value, httpClient, loggerFactory);
        _logger = loggerFactory.CreateLogger<MainModel>();
    }

    public async Task<Report> RunAsync(CancellationToken cancellationToken)
    {
        LogProcessingProject(_projectRunner.OutputDirectory);
        var reportFuture = _projectRunner.RunAsync(cancellationToken);
        try
        {
            return await reportFuture.ConfigureAwait(false);
        }
        finally
        {
            LogProcessedProject(_projectRunner.OutputDirectory);
        }
    }
}
