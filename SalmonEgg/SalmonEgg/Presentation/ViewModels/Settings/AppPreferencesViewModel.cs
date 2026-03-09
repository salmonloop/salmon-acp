using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public partial class AppPreferencesViewModel : ObservableObject
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILogger<AppPreferencesViewModel> _logger;
    private readonly SynchronizationContext _syncContext;
    private CancellationTokenSource? _saveCts;
    private bool _suppressSave;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private bool _isAnimationEnabled = true;

    [ObservableProperty]
    private string _backdrop = "System";

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private string _language = "System";

    [ObservableProperty]
    private string? _lastSelectedServerId;

    public AppPreferencesViewModel(IAppSettingsService appSettingsService, ILogger<AppPreferencesViewModel> logger)
    {
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _suppressSave = true;
            var settings = await _appSettingsService.LoadAsync();
            _syncContext.Post(_ =>
            {
                Theme = settings.Theme;
                IsAnimationEnabled = settings.IsAnimationEnabled;
                Backdrop = settings.Backdrop;
                LaunchOnStartup = settings.LaunchOnStartup;
                MinimizeToTray = settings.MinimizeToTray;
                Language = settings.Language;
                LastSelectedServerId = settings.LastSelectedServerId;
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load app settings");
        }
        finally
        {
            _suppressSave = false;
        }
    }

    partial void OnThemeChanged(string value) => ScheduleSave();
    partial void OnIsAnimationEnabledChanged(bool value) => ScheduleSave();
    partial void OnBackdropChanged(string value) => ScheduleSave();
    partial void OnLaunchOnStartupChanged(bool value) => ScheduleSave();
    partial void OnMinimizeToTrayChanged(bool value) => ScheduleSave();
    partial void OnLanguageChanged(string value) => ScheduleSave();
    partial void OnLastSelectedServerIdChanged(string? value) => ScheduleSave();

    private void ScheduleSave()
    {
        if (_suppressSave)
        {
            return;
        }

        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(750), token).ConfigureAwait(false);
                await _appSettingsService.SaveAsync(new AppSettings
                {
                    Theme = Theme,
                    IsAnimationEnabled = IsAnimationEnabled,
                    Backdrop = Backdrop,
                    LaunchOnStartup = LaunchOnStartup,
                    MinimizeToTray = MinimizeToTray,
                    Language = Language,
                    LastSelectedServerId = LastSelectedServerId
                }).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save app settings");
            }
        }, token);
    }
}
