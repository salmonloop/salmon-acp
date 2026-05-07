using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SalmonEgg.Domain.Models.Content;
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

    public static readonly DependencyProperty ToolCallContentProperty =
        DependencyProperty.Register(nameof(ToolCallContent), typeof(IReadOnlyList<ToolCallContent>), typeof(ToolCallPill), new PropertyMetadata(null, OnDisplayInputChanged));

    public static readonly DependencyProperty ToolCallLocationsProperty =
        DependencyProperty.Register(nameof(ToolCallLocations), typeof(IReadOnlyList<ToolCallLocation>), typeof(ToolCallPill), new PropertyMetadata(null, OnDisplayInputChanged));

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

    public ObservableCollection<ToolCallDisplayItem> DisplayItems { get; } = new();

    public ObservableCollection<ToolCallLocationDisplayItem> LocationItems { get; } = new();

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

    public IReadOnlyList<ToolCallContent>? ToolCallContent
    {
        get => (IReadOnlyList<ToolCallContent>?)GetValue(ToolCallContentProperty);
        set => SetValue(ToolCallContentProperty, value);
    }

    public IReadOnlyList<ToolCallLocation>? ToolCallLocations
    {
        get => (IReadOnlyList<ToolCallLocation>?)GetValue(ToolCallLocationsProperty);
        set => SetValue(ToolCallLocationsProperty, value);
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

    public bool HasDisplayItems => DisplayItems.Count > 0;

    public bool HasLocationItems => LocationItems.Count > 0;

    public bool HasPendingPermissionRequest => PendingPermissionRequest != null;

    public PermissionOptionViewModel? AllowPermissionOption => FindPermissionOption("allow");

    public PermissionOptionViewModel? RejectPermissionOption => FindPermissionOption("reject");

    public bool HasAllowPermissionOption => AllowPermissionOption != null;

    public bool HasRejectPermissionOption => RejectPermissionOption != null;

    public bool HasInlineContent => HasPendingPermissionRequest || HasDisplayItems || HasLocationItems;

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
        OnPropertyChanged(nameof(HasLocationItems));
        OnPropertyChanged(nameof(HasPendingPermissionRequest));
        OnPropertyChanged(nameof(AllowPermissionOption));
        OnPropertyChanged(nameof(RejectPermissionOption));
        OnPropertyChanged(nameof(HasAllowPermissionOption));
        OnPropertyChanged(nameof(HasRejectPermissionOption));
        OnPropertyChanged(nameof(HasInlineContent));
        OnPropertyChanged(nameof(PreviewMaxHeight));
        OnPropertyChanged(nameof(InlineContentVisibility));
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
        DisplayItems.Clear();
        LocationItems.Clear();

        AppendStructuredContentItems(ToolCallContent);
        AppendLocationItems(ToolCallLocations);

        if (DisplayItems.Count == 0)
        {
            AppendRawPayloadItems(RawPayload);
        }

        NotifyInlineContentChanged();
    }

    private void AppendStructuredContentItems(IReadOnlyList<ToolCallContent>? content)
    {
        if (content is null)
        {
            return;
        }

        foreach (var item in content)
        {
            switch (item)
            {
                case ContentToolCallContent { Content: TextContentBlock textBlock }:
                    AddTextItem(textBlock.Text);
                    break;
                case ContentToolCallContent { Content: ResourceLinkContentBlock resourceLink }:
                    AddLocationItem(resourceLink.Uri, null);
                    break;
                case DiffToolCallContent diff:
                    AddDiffItem(diff.Path, diff.OldText, diff.NewText);
                    break;
                case TerminalToolCallContent terminal:
                    AddTerminalItem(terminal.TerminalId, string.Empty);
                    break;
            }
        }
    }

    private void AppendRawPayloadItems(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            AppendJsonElement(document.RootElement);
        }
        catch (JsonException)
        {
            AddTextItem(rawPayload);
        }
    }

    private void AppendJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AppendJsonElement(item);
                }
                break;
            case JsonValueKind.Object:
                AppendJsonObject(element);
                break;
            default:
                AddTextItem(element.GetRawText());
                break;
        }
    }

    private void AppendJsonObject(JsonElement element)
    {
        var type = TryGetString(element, "type");
        switch (type)
        {
            case "content":
                if (element.TryGetProperty("content", out var content))
                {
                    AppendContentBlockJson(content);
                }
                return;
            case "diff":
                AddDiffItem(
                    TryGetString(element, "path"),
                    TryGetString(element, "oldText", "old_text"),
                    TryGetString(element, "newText", "new_text"));
                return;
            case "terminal":
                AddTerminalItem(
                    TryGetString(element, "terminalId", "terminal_id"),
                    TryGetString(element, "output"));
                return;
        }

        if (element.TryGetProperty("locations", out var locations) && locations.ValueKind == JsonValueKind.Array)
        {
            foreach (var location in locations.EnumerateArray())
            {
                AddLocationItem(TryGetString(location, "path"), TryGetInt(location, "line"));
            }
        }

        AddTextItem(SummarizePlainText(element.GetRawText()));
    }

    private void AppendContentBlockJson(JsonElement content)
    {
        var contentType = TryGetString(content, "type");
        switch (contentType)
        {
            case "text":
                AddTextItem(TryGetString(content, "text"));
                break;
            case "resource_link":
            case "resource":
                AddLocationItem(TryGetString(content, "uri"), null);
                break;
            default:
                AddTextItem(SummarizePlainText(content.GetRawText()));
                break;
        }
    }

    private void AppendLocationItems(IReadOnlyList<ToolCallLocation>? locations)
    {
        if (locations is null)
        {
            return;
        }

        foreach (var location in locations)
        {
            AddLocationItem(location.Path, location.Line);
        }
    }

    private void AddTextItem(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        DisplayItems.Add(ToolCallDisplayItem.CreateText(text.Trim()));
    }

    private void AddDiffItem(string? path, string? oldText, string? newText)
    {
        if (string.IsNullOrWhiteSpace(path)
            && string.IsNullOrWhiteSpace(oldText)
            && string.IsNullOrWhiteSpace(newText))
        {
            return;
        }

        DisplayItems.Add(ToolCallDisplayItem.CreateDiff(path, oldText, newText));
    }

    private void AddTerminalItem(string? terminalId, string? output)
    {
        if (string.IsNullOrWhiteSpace(terminalId) && string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        DisplayItems.Add(ToolCallDisplayItem.CreateTerminal(terminalId, output));
    }

    private void AddLocationItem(string? path, int? line)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        LocationItems.Add(new ToolCallLocationDisplayItem
        {
            Path = path,
            Line = line
        });
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

    private static int? TryGetInt(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            {
                return value;
            }
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
}

public sealed class ToolCallContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? DiffTemplate { get; set; }

    public DataTemplate? TerminalTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item is ToolCallDisplayItem displayItem
            ? displayItem.Kind switch
            {
                ToolCallDisplayKind.Diff => DiffTemplate,
                ToolCallDisplayKind.Terminal => TerminalTemplate,
                _ => TextTemplate
            }
            : TextTemplate;
    }
}

public sealed class ToolCallLocationDisplayItem
{
    public string Path { get; set; } = string.Empty;

    public int? Line { get; set; }

    public string DisplayText => Line is null ? Path : $"{Path}:{Line}";
}

public sealed class ToolCallDisplayItem
{
    public ToolCallDisplayKind Kind { get; set; }

    public string? Text { get; set; }

    public string? Path { get; set; }

    public string? OldText { get; set; }

    public string? NewText { get; set; }

    public string? TerminalId { get; set; }

    public static ToolCallDisplayItem CreateText(string text)
        => new()
        {
            Kind = ToolCallDisplayKind.Text,
            Text = text
        };

    public static ToolCallDisplayItem CreateDiff(string? path, string? oldText, string? newText)
        => new()
        {
            Kind = ToolCallDisplayKind.Diff,
            Path = path,
            OldText = oldText,
            NewText = newText
        };

    public static ToolCallDisplayItem CreateTerminal(string? terminalId, string? output)
        => new()
        {
            Kind = ToolCallDisplayKind.Terminal,
            Text = output,
            TerminalId = terminalId
        };

    public bool HasPath => !string.IsNullOrWhiteSpace(Path);

    public bool HasOldText => !string.IsNullOrWhiteSpace(OldText);

    public bool HasNewText => !string.IsNullOrWhiteSpace(NewText);

    public bool HasTerminalId => !string.IsNullOrWhiteSpace(TerminalId);
}

public enum ToolCallDisplayKind
{
    Text,
    Diff,
    Terminal
}
