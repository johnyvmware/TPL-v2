using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace TransactionProcessingSystem.Components;

public interface IProcessor<TInput, TOutput>
{
    ITargetBlock<TInput> InputBlock { get; }
    ISourceBlock<TOutput> OutputBlock { get; }
    Task Completion { get; }
    void Complete();
}

public abstract class ProcessorBase<TInput, TOutput> : IProcessor<TInput, TOutput>
{
    protected readonly TransformBlock<TInput, TOutput> _block;
    protected readonly CancellationTokenSource _cancellationTokenSource;
    protected readonly ILogger _logger;

    protected ProcessorBase(ILogger logger, int boundedCapacity = 100)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();

        var options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = boundedCapacity,
            CancellationToken = _cancellationTokenSource.Token,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        _block = new TransformBlock<TInput, TOutput>(
            async input => await ProcessAsync(input),
            options);
    }

    public ITargetBlock<TInput> InputBlock => _block;
    public ISourceBlock<TOutput> OutputBlock => _block;
    public Task Completion => _block.Completion;

    public void Complete()
    {
        _block.Complete();
    }

    protected abstract Task<TOutput> ProcessAsync(TInput input);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}