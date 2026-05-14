using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models.Diagnostics;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public partial class DataStorageSettingsViewModel : ObservableObject
{
    private readonly IAppDataService _paths;
    private readonly IAppMaintenanceService _maintenance;
    private readonly IDiagnosticsBundleService _diagnostics;
    private readonly IPlatformShellService _shell;
    private readonly IStorageLocationService _storageLocations;
    private readonly ISessionExportService _sessionExport;
    private readonly ILogger<DataStorageSettingsViewModel> _logger;

    public AppPreferencesViewModel Preferences { get; }
    public ChatViewModel Chat { get; }

    public string AppDataRootPath => _paths.AppDataRootPath;
    public string LogsDirectoryPath => _paths.LogsDirectoryPath;
    public string CacheRootPath => _paths.CacheRootPath;
    public string ExportsDirectoryPath => _paths.ExportsDirectoryPath;

    public DataStorageSettingsViewModel(
        AppPreferencesViewModel preferences,
        ChatViewModel chatViewModel,
        IAppDataService paths,
        IAppMaintenanceService maintenance,
        IDiagnosticsBundleService diagnostics,
        IPlatformShellService shell,
        IStorageLocationService storageLocations,
        ISessionExportService sessionExport,
        ILogger<DataStorageSettingsViewModel> logger)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Chat = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _storageLocations = storageLocations ?? throw new ArgumentNullException(nameof(storageLocations));
        _sessionExport = sessionExport ?? throw new ArgumentNullException(nameof(sessionExport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand]
    private Task OpenAppDataFolderAsync() => _storageLocations.OpenAsync(AppStorageLocation.AppData);

    [RelayCommand]
    private Task OpenCacheFolderAsync() => _storageLocations.OpenAsync(AppStorageLocation.Cache);

    [RelayCommand]
    private Task OpenLogsFolderAsync() => _storageLocations.OpenAsync(AppStorageLocation.Logs);

    [RelayCommand]
    private Task OpenExportsFolderAsync() => _storageLocations.OpenAsync(AppStorageLocation.Exports);

    [RelayCommand]
    private async Task ExportCurrentSessionMarkdownAsync()
    {
        await ExportCurrentSessionAsync("md");
    }

    [RelayCommand]
    private async Task ExportCurrentSessionJsonAsync()
    {
        await ExportCurrentSessionAsync("json");
    }

    private async Task ExportCurrentSessionAsync(string format)
    {
        try
        {
            var transcript = await Chat.GetCurrentSessionTranscriptSnapshotAsync();
            var request = new SessionExportRequest(
                format,
                Chat.CurrentSessionId,
                Chat.AgentName,
                Chat.AgentVersion,
                transcript.Select(m => new SessionExportMessage(
                    m.Id,
                    ToExportTimestamp(m.Timestamp),
                    m.IsOutgoing,
                    m.ContentType,
                    m.Title,
                    m.TextContent)).ToList());

            var path = await _sessionExport.ExportAsync(request);
            await _shell.OpenFileAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportCurrentSession failed");
        }
    }

    [RelayCommand]
    private async Task CreateDiagnosticsBundleAsync()
    {
        try
        {
            var appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "unknown";
            var snapshot = new DiagnosticsSnapshot
            {
                AppVersion = appVersion,
                ProtocolVersion = new InitializeParams().ProtocolVersion.ToString(),
                OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                Properties =
                {
                    ["AgentName"] = Chat.AgentName ?? string.Empty,
                    ["AgentVersion"] = Chat.AgentVersion ?? string.Empty,
                    ["IsConnected"] = Chat.IsConnected.ToString(),
                    ["CurrentSessionId"] = Chat.CurrentSessionId ?? string.Empty,
                }
            };

            var zipPath = await _diagnostics.CreateBundleAsync(snapshot);
            await _shell.OpenFileAsync(zipPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateDiagnosticsBundle failed");
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _maintenance.ClearCacheAsync();
    }

    [RelayCommand]
    private async Task ClearAllLocalDataAsync()
    {
        await _maintenance.ClearAllLocalDataAsync();
    }

    private static DateTimeOffset ToExportTimestamp(DateTime timestamp)
    {
        var utc = timestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
            : timestamp.ToUniversalTime();
        return new DateTimeOffset(utc);
    }
}
