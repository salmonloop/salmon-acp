using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using SalmonEgg.Presentation.Core.Services;

namespace SalmonEgg.Presentation.Services;

public class WinUiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue _queue;
    private readonly ILogger<WinUiDispatcher> _logger;

    public WinUiDispatcher(DispatcherQueue queue, ILogger<WinUiDispatcher> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue), "DispatcherQueue cannot be null. Ensure WinUiDispatcher is initialized on a thread with a dispatcher.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool HasThreadAccess => _queue.HasThreadAccess;

    public void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var success = _queue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in enqueued UI action.");
                throw;
            }
        });

        if (!success)
        {
            _logger.LogWarning("Failed to enqueue action to DispatcherQueue. The queue might be shutting down.");
            throw new InvalidOperationException("Failed to enqueue action to DispatcherQueue. The queue might be shutting down.");
        }
    }

    public Task EnqueueAsync(Action action)
    {
        if (HasThreadAccess)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        var tcs = new TaskCompletionSource<bool>();
        var success = _queue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            try
            {
                action();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in enqueued async Action.");
                tcs.TrySetException(ex);
            }
        });

        if (!success)
        {
            _logger.LogWarning("Failed to enqueue async action to DispatcherQueue.");
            return Task.FromException(new InvalidOperationException("Failed to enqueue action to DispatcherQueue. The queue might be shutting down."));
        }

        return tcs.Task;
    }

    public async Task EnqueueAsync(Func<Task> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (HasThreadAccess)
        {
            await function().ConfigureAwait(false);
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var success = _queue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            // Start the async function but do NOT await it inside the DispatcherQueueHandler.
            // The handler must be synchronous (not async void) to avoid unobserved exceptions.
            var task = function();
            if (task.IsCompleted)
            {
                // Fast path: function completed synchronously
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception?.InnerException, "Exception in enqueued async Func<Task>.");
                    tcs.TrySetException(task.Exception!.InnerException ?? task.Exception);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(true);
                }
            }
            else
            {
                // Slow path: function is still running — hook up continuation
                // that will signal the TCS when it completes.
                _ = task.ContinueWith(static (completedTask, state) =>
                {
                    var (tcs, logger) = ((TaskCompletionSource<bool>, ILogger<WinUiDispatcher>))state!;
                    if (completedTask.IsFaulted)
                    {
                        logger.LogError(completedTask.Exception?.InnerException, "Exception in enqueued async Func<Task>.");
                        tcs.TrySetException(completedTask.Exception!.InnerException ?? completedTask.Exception);
                    }
                    else if (completedTask.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(true);
                    }
                }, (tcs, _logger), TaskContinuationOptions.ExecuteSynchronously);
            }
        });

        if (!success)
        {
            _logger.LogWarning("Failed to enqueue async function to DispatcherQueue.");
            throw new InvalidOperationException("Failed to enqueue function to DispatcherQueue. The queue might be shutting down.");
        }

        await tcs.Task.ConfigureAwait(false);
    }
}
