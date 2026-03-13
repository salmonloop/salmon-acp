using System;
using Microsoft.UI.Dispatching;

namespace SalmonEgg.Presentation.Services;

public sealed class UiDispatcher : IUiDispatcher
{
    public bool TryEnqueue(Action action)
    {
        if (action == null)
        {
            return false;
        }

        var dispatcher = App.MainWindowInstance?.DispatcherQueue
            ?? DispatcherQueue.GetForCurrentThread();

        return dispatcher?.TryEnqueue(() => action()) ?? false;
    }
}
