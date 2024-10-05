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
    internal const int AttemptCount = 100;
    private const int ConcurrentAttemptCount = 20;

    private readonly HttpClient _httpClient;
    private readonly ChannelWriter<InAppMessage> _messageChannelWriter;
    private readonly IProgress<UriProgressReport> _progress;

    private UriRunner(
        int uriIndex,
        Uri uri,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<UriProgressReport> progress)
    {
        UriIndex = uriIndex;
        Uri = uri;
        _httpClient = httpClient;
        _messageChannelWriter = messageChannelWriter;
        _progress = progress;
    }

    private int UriIndex { get; }

    private Uri Uri { get; }

    internal static UriRunner Create(
        int uriIndex,
        Uri uri,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        IProgress<UriProgressReport> progress)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageChannelWriter);
        ArgumentNullException.ThrowIfNull(progress);

        return new(uriIndex, uri, httpClient, messageChannelWriter, progress);
    }

    internal async Task<UriCollectedData> RunAsync(CancellationToken cancellationToken)
    {
        // Minor implementation detail intended to filter duplicate messages from the channel.
        // We need to use a thread-safe collection because it is not “local” relative to the method that uses it.
        ConcurrentDictionary<Exception, bool> exceptionsWritten = new(ExceptionComparer.Instance);
        List<Task<RequestCollectedData>> futures = new(AttemptCount);
        using SemaphoreSlim semaphore = new(ConcurrentAttemptCount);
        for (int attemptIndex = 0;
             !cancellationToken.IsCancellationRequested && attemptIndex < AttemptCount;
             ++attemptIndex)
        {
            var semaphoreTask = semaphore.WaitAsync(cancellationToken);
            await semaphoreTask.ConfigureAwait(false);
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
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                await WriteExceptionAsync(exception).ConfigureAwait(false);
            }

            long endingTimestamp = Stopwatch.GetTimestamp();

            UriProgressReport progressReport = new();
            _progress.Report(progressReport);
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
