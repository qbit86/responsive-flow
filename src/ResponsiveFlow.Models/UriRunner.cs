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
        using SemaphoreSlim semaphore = new(_maxConcurrentRequests);

        await WarmupAsync(semaphore, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return UriCollectedData.Create(UriIndex, Uri, []);

        // Minor implementation detail intended to filter duplicate messages from the channel.
        // We need to use a thread-safe collection because it is not “local” relative to the method that uses it.
        ConcurrentDictionary<Exception, bool> exceptionsWritten = new(ExceptionComparer.Instance);
        List<Task<RequestCollectedData>> futures = new(AttemptCount);
        for (int i = 0; !cancellationToken.IsCancellationRequested && i < AttemptCount; ++i)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var future = RequestSingleAsync(semaphore, i, exceptionsWritten, cancellationToken);
            futures.Add(future);
        }

        var requestCollectedDataset = await Task.WhenAll(futures).ConfigureAwait(false);
        return UriCollectedData.Create(UriIndex, Uri, requestCollectedDataset);
    }

    private async Task<RequestCollectedData> RequestSingleAsync(
        SemaphoreSlim semaphore,
        int attemptIndex,
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
            catch (Exception exception)
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

    private async Task WarmupAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        const int warmupUpperBound = 20;
        List<Task<TimeSpan>> futures = new(warmupUpperBound);
        for (int i = 0; !cancellationToken.IsCancellationRequested && i < warmupUpperBound; ++i)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                var future = WarmupSingleAsync(semaphore, cts.Token);
                futures.Add(future);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        var lastDuration = TimeSpan.MaxValue;
        const int warmupLowerBound = 10;
        for (int i = 0; !cts.Token.IsCancellationRequested && i < futures.Count; ++i)
        {
            var previousDuration = lastDuration;
            var duration = await futures[i].ConfigureAwait(false);
            if (duration < TimeSpan.Zero)
                continue;
            lastDuration = duration;
            if (i >= warmupLowerBound && duration > previousDuration)
            {
                // The monotonous decline ended and oscillations began.
                await cts.CancelAsync().ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task<TimeSpan> WarmupSingleAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            long startingTimestamp = Stopwatch.GetTimestamp();
            try
            {
                var responseFuture = _httpClient.GetAsync(
                    Uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
                _ = await responseFuture.ConfigureAwait(false);
            }
            catch
            {
                return TimeSpan.MinValue;
            }

            return Stopwatch.GetElapsedTime(startingTimestamp);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
