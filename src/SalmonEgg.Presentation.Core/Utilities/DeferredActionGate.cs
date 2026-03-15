using System;
using System.Collections.Generic;

namespace SalmonEgg.Presentation.Utilities;

public sealed class DeferredActionGate<T> where T : notnull
{
    private readonly IEqualityComparer<T> _comparer;
    private T? _pendingKey;
    private Action? _pendingAction;

    public DeferredActionGate(IEqualityComparer<T>? comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public void Request(T key, Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _pendingKey = key;
        _pendingAction = action;
    }

    public bool TryConsume(T key)
    {
        if (_pendingAction == null || _pendingKey == null)
        {
            return false;
        }

        if (!_comparer.Equals(_pendingKey, key))
        {
            return false;
        }

        var action = _pendingAction;
        _pendingKey = default;
        _pendingAction = null;
        action();
        return true;
    }

    public void Clear()
    {
        _pendingKey = default;
        _pendingAction = null;
    }
}
