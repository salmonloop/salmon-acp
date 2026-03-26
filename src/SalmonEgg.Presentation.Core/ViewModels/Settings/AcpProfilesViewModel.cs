using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public partial class AcpProfilesViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly AppPreferencesViewModel _preferences;
    private readonly ILogger<AcpProfilesViewModel> _logger;
    private readonly SynchronizationContext _syncContext;

    [ObservableProperty]
    private ObservableCollection<ServerConfiguration> _profiles = new();

    [ObservableProperty]
    private ServerConfiguration? _selectedProfile;

    [ObservableProperty]
    private bool _isLoading;

    public AcpProfilesViewModel(
        IConfigurationService configurationService,
        AppPreferencesViewModel preferences,
        ILogger<AcpProfilesViewModel> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public async Task RefreshIfEmptyAsync()
    {
        if (Profiles.Count == 0 && !IsLoading)
        {
            await RefreshAsync().ConfigureAwait(false);
        }
    }

    public void MarkLastConnected(ServerConfiguration? profile)
    {
        _preferences.LastSelectedServerId = profile?.Id;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var configs = await _configurationService.ListConfigurationsAsync().ConfigureAwait(false);
            var ordered = configs.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToArray();

            var tcs = new TaskCompletionSource<bool>();
            
            // Re-capture current sync context if possible, or use the one from constructor
            var syncContext = SynchronizationContext.Current ?? _syncContext;
            
            syncContext.Post(_ =>
            {
                try
                {
                    Profiles.Clear();
                    foreach (var cfg in ordered)
                    {
                        Profiles.Add(cfg);
                    }

                    if (!string.IsNullOrWhiteSpace(_preferences.LastSelectedServerId))
                    {
                        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == _preferences.LastSelectedServerId);
                    }
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update profiles on UI thread");
                    tcs.TrySetException(ex);
                }
            }, null);

            await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh server profiles");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(ServerConfiguration? profile)
    {
        if (profile == null)
        {
            return;
        }

        try
        {
            await _configurationService.DeleteConfigurationAsync(profile.Id).ConfigureAwait(false);
            
            var syncContext = SynchronizationContext.Current ?? _syncContext;
            syncContext.Post(_ =>
            {
                Profiles.Remove(profile);
                if (SelectedProfile?.Id == profile.Id)
                {
                    SelectedProfile = null;
                }
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete server profile {ProfileId}", profile.Id);
        }
    }

    public async Task SaveAsync(ServerConfiguration profile)
    {
        if (profile == null)
        {
            return;
        }

        try
        {
            await _configurationService.SaveConfigurationAsync(profile).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
            SelectById(profile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save server profile {ProfileId}", profile.Id);
        }
    }

    public async Task SaveNewAsync(ServerConfiguration profile)
    {
        if (profile == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = Guid.NewGuid().ToString();
        }

        await SaveAsync(profile).ConfigureAwait(false);
    }

    private void SelectById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var syncContext = SynchronizationContext.Current ?? _syncContext;
        syncContext.Post(_ =>
        {
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == id);
        }, null);
    }
}
