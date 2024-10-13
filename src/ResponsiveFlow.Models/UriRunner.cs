using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class UriRunner
{
    internal const int AttemptCount = 100;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly int _maxConcurrentRequests;
    private readonly IProgress<UriProgressReport> _progress;

    internal UriRunner(
        int uriIndex,
        Uri uri,
        HttpClient httpClient,
        int maxConcurrentRequests,
        IProgress<UriProgressReport> progress,
        ILogger logger)
    {
        Debug.Assert(maxConcurrentRequests >= 1);

        UriIndex = uriIndex;
        Uri = uri;
        _httpClient = httpClient;
        _maxConcurrentRequests = maxConcurrentRequests;
        _progress = progress;
        _logger = logger;
    }

    private int UriIndex { get; }

    private Uri Uri { get; }

    internal async Task<UriCollectedData> RunAsync(CancellationToken cancellationToken)
    {
        // Minor implementation detail intended to filter duplicate messages from the channel.
        // We need to use a thread-safe collection because it is not “local” relative to the method that uses it.
        ConcurrentDictionary<Exception, bool> exceptionsWritten = new(ExceptionComparer.Instance);
        List<Task<RequestCollectedData>> futures = new(AttemptCount);
        using SemaphoreSlim semaphore = new(_maxConcurrentRequests);
        for (int attemptIndex = 0;
             !cancellationToken.IsCancellationRequested && attemptIndex < AttemptCount;
             ++attemptIndex)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            var future = RequestAsync(attemptIndex, semaphore, exceptionsWritten, cancellationToken);
            futures.Add(future);
        }

        var requestCollectedDataset = await Task.WhenAll(futures).ConfigureAwait(false);
        return UriCollectedData.Create(UriIndex, Uri, requestCollectedDataset);
    }

    private async Task<RequestCollectedData> RequestAsync(
        int attemptIndex,
        SemaphoreSlim semaphore,
        ConcurrentDictionary<Exception, bool> exceptionsWritten,
        CancellationToken cancellationToken)
    {
        try
        {
            long startingTimestamp = Stopwatch.GetTimestamp();
            var responseFuture = _httpClient.GetAsync(
                Uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
            try
            {
                _ = await responseFuture.ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (exceptionsWritten.TryAdd(exception, true))
                {
                    LogException(exception);
                    _progress.Report(new(exception));
                }
            }

            long endingTimestamp = Stopwatch.GetTimestamp();
            _progress.Report(new());
            return new(UriIndex, Uri, attemptIndex, startingTimestamp, endingTimestamp, responseFuture);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
