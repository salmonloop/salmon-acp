using System;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Views.MiniWindow;

public sealed partial class MiniChatView : Page
{
    public ChatViewModel ViewModel { get; }
    private bool _isLoaded;
    private ScrollViewer? _scrollViewer;
    private bool _autoScroll = true;

    public MiniChatView()
    {
        ViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        ViewModel.MessageHistory.CollectionChanged += OnMessageHistoryChanged;

        try
        {
            await ViewModel.RestoreConversationsAsync();
        }
        catch
        {
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        ViewModel.MessageHistory.CollectionChanged -= OnMessageHistoryChanged;

        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            _scrollViewer = null;
        }
    }

    private void OnMessagesListLoaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = FindScrollViewer(MessagesList);
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        }
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_scrollViewer == null)
        {
            return;
        }

        var verticalOffset = _scrollViewer.VerticalOffset;
        var maxOffset = _scrollViewer.ScrollableHeight;
        _autoScroll = verticalOffset >= maxOffset - 10;
    }

    private void OnMessageHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isLoaded)
        {
            return;
        }

        if (_autoScroll)
        {
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        try
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ChangeView(null, _scrollViewer.ScrollableHeight, null);
                return;
            }

            if (ViewModel.MessageHistory.Count > 0)
            {
                MessagesList.ScrollIntoView(ViewModel.MessageHistory[^1]);
            }
        }
        catch
        {
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer sv)
        {
            return sv;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            var result = FindScrollViewer(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
