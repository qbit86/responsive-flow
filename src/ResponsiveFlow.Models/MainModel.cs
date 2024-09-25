using System;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed class MainModel
{
    private readonly ILogger _logger;

    public MainModel(ILogger<MainModel> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }
}
