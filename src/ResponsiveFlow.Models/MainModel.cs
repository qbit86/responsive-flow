using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly ILogger _logger;
    private readonly ProjectRunner _projectRunner;

    public MainModel(IOptions<ProjectDto> projectDto, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _projectRunner = ProjectRunner.Create(projectDto.Value, loggerFactory);
        _logger = loggerFactory.CreateLogger<MainModel>();
    }

    public Task<Report> RunAsync(CancellationToken cancellationToken)
    {
        LogOutputDirectory(_projectRunner.OutputDirectory);
        return _projectRunner.RunAsync(cancellationToken);
    }
}
