using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Chat.Transcript;

namespace SalmonEgg.Presentation.Collections;

public sealed class ChatTranscriptItemsSourceAdapter :
    IList,
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IItemsRangeInfo,
    IDisposable
{
    private ChatTranscriptVirtualizedMessageCollection? _source;

    public ChatTranscriptItemsSourceAdapter(ChatTranscriptVirtualizedMessageCollection source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatTranscriptVirtualizedMessageCollection? Source
    {
        get => _source;
        set
        {
            if (ReferenceEquals(_source, value))
            {
                return;
            }

            if (_source is not null)
            {
                _source.CollectionChanged -= OnSourceCollectionChanged;
                _source.PropertyChanged -= OnSourcePropertyChanged;
            }

            _source = value;

            if (_source is not null)
            {
                _source.CollectionChanged += OnSourceCollectionChanged;
                _source.PropertyChanged += OnSourcePropertyChanged;
            }

            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
            CollectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public int Count => _source?.Count ?? 0;

    public bool IsFixedSize => true;

    public bool IsReadOnly => true;

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    object? IList.this[int index]
    {
        get
        {
            if (_source is null)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _source[index];
        }
        set => throw new NotSupportedException();
    }

    public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
    {
        if (_source is null)
        {
            return;
        }

        var trackedRanges = new TranscriptVirtualizationRange[trackedItems.Count];
        for (var index = 0; index < trackedItems.Count; index++)
        {
            trackedRanges[index] = ConvertRange(trackedItems[index]);
        }

        _source.ApplyRequiredRanges(ConvertRange(visibleRange), trackedRanges);
    }

    public int Add(object? value) => throw new NotSupportedException();

    public void Clear() => throw new NotSupportedException();

    public bool Contains(object? value) => IndexOf(value) >= 0;

    public int IndexOf(object? value)
        => value is ChatMessageViewModel message && _source is not null
            ? _source.IndexOf(message)
            : -1;

    public void Insert(int index, object? value) => throw new NotSupportedException();

    public void Remove(object? value) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public void CopyTo(Array array, int index)
    {
        if (_source is null)
        {
            return;
        }

        _source.CopyTo(array, index);
    }

    public IEnumerator GetEnumerator()
    {
        if (_source is null)
        {
            yield break;
        }

        for (var index = 0; index < _source.Count; index++)
        {
            yield return _source[index];
        }
    }

    public void Dispose()
    {
        Source = null;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static TranscriptVirtualizationRange ConvertRange(ItemIndexRange range)
        => new(range.FirstIndex, range.Length > int.MaxValue ? int.MaxValue : (int)range.Length);
}
