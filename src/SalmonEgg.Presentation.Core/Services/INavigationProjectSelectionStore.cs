using System;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.Core.Services;

public interface INavigationProjectSelectionStore
{
    void RememberSelectedProject(string? projectId);
}

public sealed class NavigationProjectSelectionStoreAdapter : INavigationProjectSelectionStore
{
    private readonly AppPreferencesViewModel _preferences;

    public NavigationProjectSelectionStoreAdapter(AppPreferencesViewModel preferences)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    public void RememberSelectedProject(string? projectId)
    {
        _preferences.LastSelectedProjectId = string.Equals(projectId, NavigationProjectIds.Unclassified, StringComparison.Ordinal)
            ? null
            : projectId;
    }
}
