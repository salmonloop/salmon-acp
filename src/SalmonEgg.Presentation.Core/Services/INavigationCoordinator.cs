using System.Threading.Tasks;
using SalmonEgg.Presentation.Models.Navigation;

namespace SalmonEgg.Presentation.Core.Services;

public interface INavigationCoordinator
{
    Task<bool> ActivateStartAsync(string? projectIdForNewSession = null);

    Task ActivateDiscoverSessionsAsync();

    Task ActivateSettingsAsync(string settingsKey);

    Task<bool> ActivateSessionAsync(string sessionId, string? projectId);

    void SyncSelectionFromShellContent(ShellNavigationContent content);
}
