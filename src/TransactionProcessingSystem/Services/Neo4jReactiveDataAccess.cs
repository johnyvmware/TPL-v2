using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Modern reactive Neo4j data access implementation using latest C# features
/// Combines IAsyncEnumerable with System.Reactive for optimal streaming performance
/// Primary constructor pattern with advanced backpressure control using channels
/// </summary>
public sealed class Neo4jReactiveDataAccess(
    INeo4jDataAccess dataAccess,
    ILogger<Neo4jReactiveDataAccess> logger) : INeo4jReactiveDataAccess, IAsyncDisposable
{
    private readonly Subject<TransactionAnalytics> _analyticsSubject = new();
    private readonly Subject<ConnectivityStatus> _connectivitySubject = new();
    private bool _disposed;

    public IObservable<TransactionResult> UpsertTransactionsReactive(
        IAsyncEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<TransactionResult>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                logger.LogDebug("Starting reactive transaction upsert stream");

                await foreach (var result in dataAccess.UpsertTransactionsAsync(transactions, linkedToken)
                    .WithCancellation(linkedToken).ConfigureAwait(false))
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    observer.OnNext(result);
                    
                    if (result.IsSuccess)
                    {
                        logger.LogTrace("Reactively processed transaction: {TransactionId}", result.TransactionId);
                    }
                    else
                    {
                        logger.LogWarning("Reactive processing failed for transaction {TransactionId}: {Error}",
                            result.TransactionId, result.ErrorMessage);
                    }
                }

                observer.OnCompleted();
                logger.LogDebug("Completed reactive transaction upsert stream");
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                logger.LogDebug("Reactive transaction upsert stream was cancelled");
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive transaction upsert stream");
                observer.OnError(ex);
            }
        });
    }

    public IObservable<TransactionAnalytics> GetAnalyticsReactive(CancellationToken cancellationToken = default)
    {
        return Observable.Create<TransactionAnalytics>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                // Initial analytics fetch
                var analytics = await dataAccess.GetTransactionAnalyticsAsync(linkedToken).ConfigureAwait(false);
                observer.OnNext(analytics);

                // Subscribe to analytics updates (you could implement periodic updates here)
                using var subscription = _analyticsSubject
                    .TakeUntil(Observable.FromAsync(() => Task.Delay(Timeout.Infinite, linkedToken)))
                    .Subscribe(observer);

                // Keep the observable alive until cancellation
                await Task.Delay(Timeout.Infinite, linkedToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive analytics stream");
                observer.OnError(ex);
            }
        });
    }

    public IObservable<Transaction> FindSimilarTransactionsReactive(
        Transaction referenceTransaction,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<Transaction>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                logger.LogDebug("Starting reactive similar transactions search for {TransactionId}", 
                    referenceTransaction.Id);

                await foreach (var transaction in dataAccess.FindSimilarTransactionsAsync(referenceTransaction, linkedToken)
                    .WithCancellation(linkedToken).ConfigureAwait(false))
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    observer.OnNext(transaction);
                    logger.LogTrace("Found similar transaction: {SimilarId}", transaction.Id);
                }

                observer.OnCompleted();
                logger.LogDebug("Completed reactive similar transactions search");
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive similar transactions stream");
                observer.OnError(ex);
            }
        });
    }

    public IObservable<IDictionary<string, object>> ExecuteQueryReactive(
        string cypher,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<IDictionary<string, object>>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                logger.LogDebug("Executing reactive Cypher query");

                await foreach (var result in dataAccess.ExecuteQueryAsync(cypher, parameters, linkedToken)
                    .WithCancellation(linkedToken).ConfigureAwait(false))
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    observer.OnNext(result);
                }

                observer.OnCompleted();
                logger.LogDebug("Completed reactive query execution");
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive query execution");
                observer.OnError(ex);
            }
        });
    }

    public IObservable<GraphStatistic> GetGraphStatisticsReactive(CancellationToken cancellationToken = default)
    {
        return Observable.Create<GraphStatistic>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                logger.LogDebug("Starting reactive graph statistics stream");

                await foreach (var statistic in dataAccess.GetGraphStatisticsAsync(linkedToken)
                    .WithCancellation(linkedToken).ConfigureAwait(false))
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    observer.OnNext(statistic);
                    logger.LogTrace("Graph statistic: {Type} {Name} = {Count}", 
                        statistic.Type, statistic.Name, statistic.Count);
                }

                observer.OnCompleted();
                logger.LogDebug("Completed reactive graph statistics stream");
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive graph statistics stream");
                observer.OnError(ex);
            }
        });
    }

    public IObservable<ConnectivityStatus> VerifyConnectivityReactive(CancellationToken cancellationToken = default)
    {
        return Observable.Create<ConnectivityStatus>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
            var linkedToken = linkedCts.Token;

            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        var isConnected = await dataAccess.VerifyConnectivityAsync(linkedToken).ConfigureAwait(false);
                        stopwatch.Stop();

                        var status = new ConnectivityStatus(
                            isConnected,
                            stopwatch.Elapsed,
                            isConnected ? null : "Connection failed");

                        observer.OnNext(status);
                        _connectivitySubject.OnNext(status);

                        logger.LogTrace("Connectivity check: {IsConnected} in {ResponseTime}ms", 
                            isConnected, stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        var status = new ConnectivityStatus(false, stopwatch.Elapsed, ex.Message);
                        observer.OnNext(status);
                        _connectivitySubject.OnNext(status);

                        logger.LogWarning(ex, "Connectivity check failed");
                    }

                    // Wait before next check (configurable interval)
                    await Task.Delay(TimeSpan.FromSeconds(30), linkedToken).ConfigureAwait(false);
                }

                observer.OnCompleted();
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in reactive connectivity monitoring");
                observer.OnError(ex);
            }
        });
    }

    public async ValueTask<ChannelWriter<Transaction>> CreateTransactionChannelAsync(
        int capacity = 1000,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating transaction channel with capacity {Capacity} and full mode {FullMode}", 
            capacity, fullMode);

        var channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = fullMode,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        var channel = Channel.CreateBounded<Transaction>(channelOptions);

        // Start background processing of the channel
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessChannelAsync(channel.Reader, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing transaction channel");
            }
        }, cancellationToken);

        return channel.Writer;
    }

    private async Task ProcessChannelAsync(ChannelReader<Transaction> reader, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting channel processing");

        try
        {
            // Convert channel to async enumerable and process
            var transactions = reader.ReadAllAsync(cancellationToken);
            
            await foreach (var result in dataAccess.UpsertTransactionsAsync(transactions, cancellationToken))
            {
                if (result.IsSuccess)
                {
                    logger.LogTrace("Channel processed transaction: {TransactionId}", result.TransactionId);
                }
                else
                {
                    logger.LogWarning("Channel processing failed for transaction {TransactionId}: {Error}",
                        result.TransactionId, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in channel processing");
            throw;
        }
        finally
        {
            logger.LogDebug("Channel processing completed");
        }
    }

    /// <summary>
    /// Publishes analytics updates to reactive subscribers
    /// </summary>
    public async ValueTask PublishAnalyticsUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await dataAccess.GetTransactionAnalyticsAsync(cancellationToken).ConfigureAwait(false);
            _analyticsSubject.OnNext(analytics);
            logger.LogDebug("Published analytics update to reactive subscribers");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish analytics update");
            _analyticsSubject.OnError(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            logger.LogDebug("Disposing reactive Neo4j data access");

            try
            {
                _analyticsSubject.OnCompleted();
                _connectivitySubject.OnCompleted();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error completing reactive subjects during disposal");
            }
            finally
            {
                _analyticsSubject.Dispose();
                _connectivitySubject.Dispose();
            }

            _disposed = true;
            logger.LogDebug("Reactive Neo4j data access disposed");
        }
    }
}