using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ResponsiveFlow;

internal sealed class UriRunner
{
    private const int ConcurrentRequestCount = 20;
    private const int RequestCount = 100;

    private readonly HttpClient _httpClient;
    private readonly ChannelWriter<InAppMessage> _messageChannelWriter;

    private UriRunner(int uriIndex, Uri uri, HttpClient httpClient, ChannelWriter<InAppMessage> messageChannelWriter)
    {
        UriIndex = uriIndex;
        Uri = uri;
        _httpClient = httpClient;
        _messageChannelWriter = messageChannelWriter;
    }

    private int UriIndex { get; }

    private Uri Uri { get; }

    internal static UriRunner Create(
        int uriIndex, Uri uri, HttpClient httpClient, ChannelWriter<InAppMessage> messageChannelWriter)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageChannelWriter);

        return new(uriIndex, uri, httpClient, messageChannelWriter);
    }

    internal async Task<UriCollectedData> RunAsync(CancellationToken cancellationToken)
    {
        // Minor implementation detail intended to filter duplicate messages from the channel.
        // We need to use a thread-safe collection because it is not “local” relative to the method that uses it.
        ConcurrentDictionary<Exception, bool> exceptionsWritten = new(ExceptionComparer.Instance);
        List<Task<RequestCollectedData>> futures = new(RequestCount);
        using SemaphoreSlim semaphore = new(ConcurrentRequestCount);
        for (int attemptIndex = 0;
             !cancellationToken.IsCancellationRequested && attemptIndex < RequestCount;
             ++attemptIndex)
        {
            var semaphoreTask = semaphore.WaitAsync(cancellationToken);
            await semaphoreTask.ConfigureAwait(false);
            var future = RequestAsync(attemptIndex, semaphore, exceptionsWritten, cancellationToken);
            futures.Add(future);
        }

        var requestCollectedDataset = await Task.WhenAll(futures).ConfigureAwait(false);
        return new(UriIndex, Uri, requestCollectedDataset);
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
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                await WriteExceptionAsync(exception).ConfigureAwait(false);
            }

            // TODO: Refactor RequestCollectedData, get rid of wrapping task.
            var endingTimestamp = Task.FromResult(Stopwatch.GetTimestamp());
            // TODO: Report progress.

            return new(UriIndex, Uri, attemptIndex, startingTimestamp, endingTimestamp, responseFuture);
        }
        finally
        {
            _ = semaphore.Release();
        }

        ValueTask WriteExceptionAsync(Exception exception)
        {
            if (!exceptionsWritten.TryAdd(exception, true))
                return ValueTask.CompletedTask;
            return _messageChannelWriter.WriteAsync(InAppMessage.FromException(exception), cancellationToken);
        }
    }
}
