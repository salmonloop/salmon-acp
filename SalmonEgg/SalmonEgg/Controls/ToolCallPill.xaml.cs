using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Domain.Models.Tool;
using SalmonEgg.Presentation.ViewModels.Chat;
using Windows.ApplicationModel.Resources;

namespace SalmonEgg.Controls;

public sealed partial class ToolCallPill : UserControl, INotifyPropertyChanged
{
    private static readonly ResourceLoader ResourceLoader = ResourceLoader.GetForViewIndependentUse();
    private bool _isExpanded;

    public static readonly DependencyProperty ToolTitleProperty =
        DependencyProperty.Register(
            nameof(ToolTitle),
            typeof(string),
            typeof(ToolCallPill),
            new PropertyMetadata(string.Empty, OnDisplayInputChanged));

    public static readonly DependencyProperty ToolKindProperty =
        DependencyProperty.Register(
            nameof(ToolKind),
            typeof(ToolCallKind?),
            typeof(ToolCallPill),
            new PropertyMetadata(null, OnDisplayInputChanged));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(ToolCallStatus?), typeof(ToolCallPill), new PropertyMetadata(null, OnDisplayInputChanged));

    public static readonly DependencyProperty RawPayloadProperty =
        DependencyProperty.Register(nameof(RawPayload), typeof(string), typeof(ToolCallPill), new PropertyMetadata(string.Empty, OnDisplayInputChanged));

    public static readonly DependencyProperty DetailItemsProperty =
        DependencyProperty.Register(nameof(DetailItems), typeof(IReadOnlyList<ToolCallDetailItem>), typeof(ToolCallPill), new PropertyMetadata(null, OnDisplayInputChanged));

    public static readonly DependencyProperty PendingPermissionRequestProperty =
        DependencyProperty.Register(nameof(PendingPermissionRequest), typeof(PermissionRequestViewModel), typeof(ToolCallPill), new PropertyMetadata(null, OnPermissionInputChanged));

    public static readonly DependencyProperty IsInProgressProperty =
        DependencyProperty.Register(nameof(IsInProgress), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));

    public static readonly DependencyProperty IsCompletedProperty =
        DependencyProperty.Register(nameof(IsCompleted), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));

    public static readonly DependencyProperty IsFailedProperty =
        DependencyProperty.Register(nameof(IsFailed), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));

    public static readonly DependencyProperty IsCancelledProperty =
        DependencyProperty.Register(nameof(IsCancelled), typeof(bool), typeof(ToolCallPill), new PropertyMetadata(false, OnVisualStateInputChanged));

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ToolTitle
    {
        get => (string)GetValue(ToolTitleProperty);
        set => SetValue(ToolTitleProperty, value);
    }

    public ToolCallKind? ToolKind
    {
        get => (ToolCallKind?)GetValue(ToolKindProperty);
        set => SetValue(ToolKindProperty, value);
    }

    public ToolCallStatus? Status
    {
        get => (ToolCallStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string RawPayload
    {
        get => (string)GetValue(RawPayloadProperty);
        set => SetValue(RawPayloadProperty, value);
    }

    public IReadOnlyList<ToolCallDetailItem>? DetailItems
    {
        get => (IReadOnlyList<ToolCallDetailItem>?)GetValue(DetailItemsProperty);
        set => SetValue(DetailItemsProperty, value);
    }

    public PermissionRequestViewModel? PendingPermissionRequest
    {
        get => (PermissionRequestViewModel?)GetValue(PendingPermissionRequestProperty);
        set => SetValue(PendingPermissionRequestProperty, value);
    }

    public bool IsInProgress
    {
        get => (bool)GetValue(IsInProgressProperty);
        set => SetValue(IsInProgressProperty, value);
    }

    public bool IsCompleted
    {
        get => (bool)GetValue(IsCompletedProperty);
        set => SetValue(IsCompletedProperty, value);
    }

    public bool IsFailed
    {
        get => (bool)GetValue(IsFailedProperty);
        set => SetValue(IsFailedProperty, value);
    }

    public bool IsCancelled
    {
        get => (bool)GetValue(IsCancelledProperty);
        set => SetValue(IsCancelledProperty, value);
    }

    public string DisplayToolName => ResolveToolName();

    public string DisplaySummary => ResolveSummary();

    public string PayloadHeaderText => ResolveResourceString("ToolCallPillPayloadTitle", "Payload details");

    public string PermissionHeaderText => ResolveResourceString("ToolCallPillPermissionHeader", "Approval required");

    public bool HasDisplayItems => DetailItems?.Count > 0;

    public bool HasPendingPermissionRequest => PendingPermissionRequest != null;

    public PermissionOptionViewModel? AllowPermissionOption => FindPermissionOption("allow");

    public PermissionOptionViewModel? RejectPermissionOption => FindPermissionOption("reject");

    public bool HasAllowPermissionOption => AllowPermissionOption != null;

    public bool HasRejectPermissionOption => RejectPermissionOption != null;

    public bool HasInlineContent => HasPendingPermissionRequest || HasDisplayItems;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewMaxHeight));
            OnPropertyChanged(nameof(InlineContentVisibility));
            UpdateVisualStates();
            UpdateExpansionState(true);
        }
    }

    public double PreviewMaxHeight => IsExpanded ? double.PositiveInfinity : 120;

    public Visibility InlineContentVisibility =>
        HasInlineContent && (!IsCompleted || IsExpanded)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string AutomationName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplaySummary))
            {
                return DisplayToolName;
            }

            return $"{DisplayToolName}, {DisplaySummary}";
        }
    }

    public ToolCallPill()
    {
        InitializeComponent();
        UpdateDisplayProjection();
        Loaded += ToolCallPill_Loaded;
    }

    private void ToolCallPill_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVisualStates();
        UpdateExpansionState(false);
    }

    private static void OnDisplayInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolCallPill pill)
        {
            pill.UpdateDisplayProjection();
            pill.NotifyDisplayChanged();
        }
    }

    private static void OnPermissionInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolCallPill pill)
        {
            pill.NotifyInlineContentChanged();
        }
    }

    private static void OnVisualStateInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolCallPill pill)
        {
            var propertyName =
                e.Property == IsInProgressProperty ? nameof(IsInProgress) :
                e.Property == IsCompletedProperty ? nameof(IsCompleted) :
                e.Property == IsFailedProperty ? nameof(IsFailed) :
                e.Property == IsCancelledProperty ? nameof(IsCancelled) :
                null;

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                pill.OnPropertyChanged(propertyName);
                pill.OnPropertyChanged(nameof(InlineContentVisibility));
                pill.UpdateExpansionState(true);
            }
        }
    }

    private void NotifyDisplayChanged()
    {
        OnPropertyChanged(nameof(DisplayToolName));
        OnPropertyChanged(nameof(DisplaySummary));
        OnPropertyChanged(nameof(PayloadHeaderText));
        OnPropertyChanged(nameof(AutomationName));
        NotifyInlineContentChanged();
    }

    private void NotifyInlineContentChanged()
    {
        OnPropertyChanged(nameof(PermissionHeaderText));
        OnPropertyChanged(nameof(HasDisplayItems));
        OnPropertyChanged(nameof(HasPendingPermissionRequest));
        OnPropertyChanged(nameof(AllowPermissionOption));
        OnPropertyChanged(nameof(RejectPermissionOption));
        OnPropertyChanged(nameof(HasAllowPermissionOption));
        OnPropertyChanged(nameof(HasRejectPermissionOption));
        OnPropertyChanged(nameof(HasInlineContent));
        OnPropertyChanged(nameof(PreviewMaxHeight));
        OnPropertyChanged(nameof(InlineContentVisibility));
        UpdateExpansionState(true);
    }

    private string ResolveToolName()
    {
        if (!string.IsNullOrWhiteSpace(ToolTitle))
        {
            return ToolTitle;
        }

        return ToolKind switch
        {
            ToolCallKind.Read => ResolveResourceString("ToolCallPillKindRead", "Read file"),
            ToolCallKind.Edit => ResolveResourceString("ToolCallPillKindEdit", "Edit file"),
            ToolCallKind.Delete => ResolveResourceString("ToolCallPillKindDelete", "Delete file"),
            ToolCallKind.Move => ResolveResourceString("ToolCallPillKindMove", "Move file"),
            ToolCallKind.Search => ResolveResourceString("ToolCallPillKindSearch", "Search code"),
            ToolCallKind.Execute => ResolveResourceString("ToolCallPillKindExecute", "Run command"),
            ToolCallKind.SwitchMode => ResolveResourceString("ToolCallPillKindSwitchMode", "Switch mode"),
            ToolCallKind.Think => ResolveResourceString("ToolCallPillKindThink", "Thinking"),
            ToolCallKind.Fetch => ResolveResourceString("ToolCallPillKindFetch", "Fetch data"),
            _ => ResolveResourceString("ToolCallPillKindDefault", "Tool call")
        };
    }

    private string ResolveSummary()
    {
        if (string.IsNullOrWhiteSpace(RawPayload))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(RawPayload);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var contentSummary = SummarizeStructuredContentArray(root);
                if (!string.IsNullOrWhiteSpace(contentSummary))
                {
                    return contentSummary;
                }

                return SummarizePlainText(RawPayload);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return SummarizePlainText(RawPayload);
            }

            var parts = new List<string>();
            var path = TryGetString(root, "path", "Path", "SearchPath", "searchPath", "TargetFile", "targetFile");
            if (!string.IsNullOrWhiteSpace(path))
            {
                parts.Add($"{ResolveResourceString("ToolCallPillSummaryPathLabel", "Path")}: {path}");
            }

            var query = TryGetString(root, "query", "Query");
            if (!string.IsNullOrWhiteSpace(query))
            {
                parts.Add($"{ResolveResourceString("ToolCallPillSummaryQueryLabel", "Query")}: {query}");
            }

            var command = TryGetString(root, "CommandLine", "commandLine", "command", "Command", "cmd");
            var arguments = TryGetString(root, "Arguments", "arguments", "Args", "args");
            var commandSummary = BuildCommandSummary(command, arguments);
            if (!string.IsNullOrWhiteSpace(commandSummary))
            {
                parts.Add($"{ResolveResourceString("ToolCallPillSummaryCommandLabel", "Command")}: {commandSummary}");
            }

            if (parts.Count > 0)
            {
                return Truncate(string.Join(", ", parts));
            }
        }
        catch (JsonException)
        {
        }

        return SummarizePlainText(RawPayload);
    }

    private void UpdateDisplayProjection()
    {
        NotifyInlineContentChanged();
    }

    private PermissionOptionViewModel? FindPermissionOption(string kindPrefix)
    {
        var options = PendingPermissionRequest?.Options;
        if (options is null)
        {
            return null;
        }

        return options.FirstOrDefault(option =>
            option.Kind.StartsWith(kindPrefix, StringComparison.OrdinalIgnoreCase));
    }

    private string SummarizeStructuredContentArray(JsonElement root)
    {
        var parts = new List<string>();
        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var type = TryGetString(item, "type");
            switch (type)
            {
                case "content":
                    if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object)
                    {
                        var contentType = TryGetString(content, "type");
                        switch (contentType)
                        {
                            case "text":
                                var text = TryGetString(content, "text");
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    parts.Add(Truncate(text));
                                }
                                break;
                            case "resource_link":
                                var uri = TryGetString(content, "uri");
                                if (!string.IsNullOrWhiteSpace(uri))
                                {
                                    parts.Add($"{ResolveResourceString("ToolCallPillSummaryPathLabel", "Path")}: {uri}");
                                }
                                break;
                            case "resource":
                                var resourceUri = TryGetString(content, "uri");
                                if (!string.IsNullOrWhiteSpace(resourceUri))
                                {
                                    parts.Add($"{ResolveResourceString("ToolCallPillSummaryPathLabel", "Path")}: {resourceUri}");
                                }
                                break;
                            case "image":
                                var mimeType = TryGetString(content, "mimeType", "mime_type");
                                parts.Add(string.IsNullOrWhiteSpace(mimeType)
                                    ? ResolveResourceString("ToolCallPillSummaryImageContent", "Image content")
                                    : $"{ResolveResourceString("ToolCallPillSummaryImageLabel", "Image")}: {mimeType}");
                                break;
                            case "audio":
                                var audioMimeType = TryGetString(content, "mimeType", "mime_type");
                                parts.Add(string.IsNullOrWhiteSpace(audioMimeType)
                                    ? ResolveResourceString("ToolCallPillSummaryAudioContent", "Audio content")
                                    : $"{ResolveResourceString("ToolCallPillSummaryAudioLabel", "Audio")}: {audioMimeType}");
                                break;
                        }
                    }
                    break;
                case "diff":
                    var path = TryGetString(item, "path");
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        parts.Add($"{ResolveResourceString("ToolCallPillSummaryPathLabel", "Path")}: {path}");
                    }
                    break;
                case "terminal":
                    var terminalId = TryGetString(item, "terminalId", "terminal_id");
                    if (!string.IsNullOrWhiteSpace(terminalId))
                    {
                        parts.Add($"{ResolveResourceString("ToolCallPillSummaryCommandLabel", "Command")}: {terminalId}");
                    }
                    break;
            }
        }

        return parts.Count == 0 ? string.Empty : Truncate(string.Join(", ", parts));
    }

    private static string? TryGetString(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.GetRawText();
        }

        return null;
    }

    private static string? BuildCommandSummary(string? command, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.IsNullOrWhiteSpace(arguments) ? null : arguments;
        }

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return command;
        }

        return $"{command} {arguments}";
    }

    private static string SummarizePlainText(string text)
    {
        var normalized = text.Trim().Replace("\r", " ").Replace("\n", " ");
        return Truncate(normalized);
    }

    private static string Truncate(string text)
        => text.Length > 64 ? $"{text[..61]}..." : text;

    private static string ResolveResourceString(string resourceKey, string fallback)
    {
        var value = ResourceLoader.GetString(resourceKey);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async void OnPermissionOptionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PermissionOptionViewModel option }
            && PendingPermissionRequest?.RespondCommand is { } command)
        {
            await command.ExecuteAsync(option);
        }
    }

    private bool _isHovered;

    private void HeaderGrid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }

    private void HeaderGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isHovered = true;
        UpdateVisualStates();
    }

    private void HeaderGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isHovered = false;
        UpdateVisualStates();
    }

    private void HeaderGrid_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }

    private void UpdateExpansionState(bool useTransitions)
    {
        if (DetailScrollViewer == null)
        {
            return;
        }

        bool shouldShow = HasInlineContent && (!IsCompleted || IsExpanded);
        string stateName = shouldShow ? "DetailExpandedState" : "DetailCollapsedState";
        VisualStateManager.GoToState(this, stateName, useTransitions);
    }

    private void UpdateVisualStates()
    {
        if (ToolNameTextBlock == null)
        {
            return;
        }

        string brushKey;
        if (IsExpanded)
        {
            brushKey = "AccentTextFillColorPrimaryBrush";
        }
        else if (_isHovered)
        {
            brushKey = "TextFillColorPrimaryBrush";
        }
        else
        {
            brushKey = "TextFillColorSecondaryBrush";
        }

        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(brushKey, out var brushObj))
        {
            if (brushObj is Brush brush)
            {
                ToolNameTextBlock.Foreground = brush;
                return;
            }
        }

        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var fallbackObj))
        {
            if (fallbackObj is Brush fallbackBrush)
            {
                ToolNameTextBlock.Foreground = fallbackBrush;
            }
        }
    }
}
