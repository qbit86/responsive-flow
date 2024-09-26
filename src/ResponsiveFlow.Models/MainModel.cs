using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed partial class MainModel
{
    private readonly ILogger _logger;

    public MainModel(ILogger<MainModel> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; !cancellationToken.IsCancellationRequested; ++i)
        {
            await Task.Delay(5000, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            LogIteration(i);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = $"[{nameof(RunAsync)}] {{index}}")]
    private partial void LogIteration(int index);
}
