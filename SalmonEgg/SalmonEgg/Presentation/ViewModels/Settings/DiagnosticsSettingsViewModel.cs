using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models.Diagnostics;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed partial class DiagnosticsSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IAppDataService _paths;
    private readonly IDiagnosticsBundleService _bundle;
    private readonly ILogStreamService _logStreamService;
    private readonly IPlatformShellService _shell;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<DiagnosticsSettingsViewModel> _logger;
    private IDisposable? _logStreamSubscription;
    private string _lastReadLogContent = string.Empty;

    public ChatViewModel Chat { get; }

    public string AppVersion => typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";

    public string ProtocolVersion => new InitializeParams().ProtocolVersion.ToString();

    public string OsDescription => System.Runtime.InteropServices.RuntimeInformation.OSDescription;

    public string FrameworkDescription => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public string AppDataRootPath => _paths.AppDataRootPath;

    public string LogsDirectoryPath => _paths.LogsDirectoryPath;

    [ObservableProperty]
    private string? _latestLogFilePath;

    [ObservableProperty]
    private string _logViewerContent = string.Empty;

    [ObservableProperty]
    private bool _isLogStreamingEnabled;

    [ObservableProperty]
    private bool _autoScrollToBottom = true;

    public DiagnosticsSettingsViewModel(
        ChatViewModel chatViewModel,
        IAppDataService paths,
        IDiagnosticsBundleService bundle,
        ILogStreamService logStreamService,
        IPlatformShellService shell,
        IUiDispatcher uiDispatcher,
        ILogger<DiagnosticsSettingsViewModel> logger)
    {
        Chat = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        _logStreamService = logStreamService ?? throw new ArgumentNullException(nameof(logStreamService));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshLatestLogFile();
    }

    [RelayCommand]
    private void ToggleLogStreaming()
    {
        if (IsLogStreamingEnabled)
        {
            StopLogStreaming();
        }
        else
        {
            StartLogStreaming();
        }
    }

    private void StartLogStreaming()
    {
        if (string.IsNullOrWhiteSpace(LatestLogFilePath))
        {
            RefreshLatestLogFile();
            if (string.IsNullOrWhiteSpace(LatestLogFilePath))
            {
                return;
            }
        }

        try
        {
            var latestPath = LatestLogFilePath!;
            _lastReadLogContent = File.ReadAllText(latestPath);
            LogViewerContent = _lastReadLogContent;

            _logStreamSubscription = _logStreamService.StartWatching(latestPath, OnLogContentChanged);
            IsLogStreamingEnabled = _logStreamSubscription != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start log streaming");
        }
    }

    private void StopLogStreaming()
    {
        _logStreamSubscription?.Dispose();
        _logStreamSubscription = null;
        IsLogStreamingEnabled = false;
    }

    private void OnLogContentChanged(string newContent)
    {
        try
        {
            if (newContent == _lastReadLogContent)
            {
                return;
            }

            _lastReadLogContent = newContent;
            _ = _uiDispatcher.TryEnqueue(() =>
            {
                LogViewerContent = newContent;
            });
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    private void ClearLogViewer()
    {
        LogViewerContent = string.Empty;
        _lastReadLogContent = string.Empty;
    }

    [RelayCommand]
    private void RefreshLatestLogFile()
    {
        try
        {
            if (!Directory.Exists(_paths.LogsDirectoryPath))
            {
                LatestLogFilePath = null;
                return;
            }

            var latest = Directory.EnumerateFiles(_paths.LogsDirectoryPath, "*.log", SearchOption.TopDirectoryOnly)
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            LatestLogFilePath = latest?.FullName;
        }
        catch
        {
            LatestLogFilePath = null;
        }
    }

    [RelayCommand]
    private Task OpenLogsFolderAsync() => _shell.OpenFolderAsync(_paths.LogsDirectoryPath);

    [RelayCommand]
    private Task OpenAppDataFolderAsync() => _shell.OpenFolderAsync(_paths.AppDataRootPath);

    [RelayCommand]
    private async Task CopyRecentLogSnippetAsync()
    {
        try
        {
            RefreshLatestLogFile();
            if (string.IsNullOrWhiteSpace(LatestLogFilePath) || !File.Exists(LatestLogFilePath))
            {
                await _shell.CopyToClipboardAsync("No log file found.").ConfigureAwait(false);
                return;
            }

            var text = await ReadTailAsync(LatestLogFilePath, 8000).ConfigureAwait(false);
            await _shell.CopyToClipboardAsync(text).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopyRecentLogSnippet failed");
        }
    }

    [RelayCommand]
    private async Task CreateDiagnosticsBundleAsync()
    {
        try
        {
            var snapshot = new DiagnosticsSnapshot
            {
                AppVersion = AppVersion,
                ProtocolVersion = ProtocolVersion,
                OsDescription = OsDescription,
                FrameworkDescription = FrameworkDescription,
                Properties = new Dictionary<string, string>
                {
                    ["AgentName"] = Chat.AgentName ?? string.Empty,
                    ["AgentVersion"] = Chat.AgentVersion ?? string.Empty,
                    ["IsConnected"] = Chat.IsConnected.ToString(),
                    ["CurrentSessionId"] = Chat.CurrentSessionId ?? string.Empty
                }
            };

            var path = await _bundle.CreateBundleAsync(snapshot).ConfigureAwait(false);
            await _shell.OpenFileAsync(path).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateDiagnosticsBundle failed");
        }
    }

    private static async Task<string> ReadTailAsync(string filePath, int maxChars)
    {
        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
        if (text.Length <= maxChars)
        {
            return text;
        }

        return text.Substring(text.Length - maxChars, maxChars);
    }

    public void Dispose()
    {
        StopLogStreaming();
    }
}

