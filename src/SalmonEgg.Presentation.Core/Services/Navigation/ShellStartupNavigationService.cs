using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SalmonEgg.Presentation.Models.Navigation;

namespace SalmonEgg.Presentation.Core.Services;

public sealed class ShellStartupNavigationService : IShellStartupNavigationService
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILogger<ShellStartupNavigationService> _logger;
    private int _activationInFlight;
    private bool _activationCompleted;

    public ShellStartupNavigationService(
        INavigationCoordinator navigationCoordinator,
        ILogger<ShellStartupNavigationService>? logger = null)
    {
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _logger = logger ?? NullLogger<ShellStartupNavigationService>.Instance;
    }

    public async Task ActivateInitialContentAsync()
    {
        if (_activationCompleted)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _activationInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var activated = await _navigationCoordinator.ActivateStartAsync().ConfigureAwait(true);
            if (activated)
            {
                _activationCompleted = true;
                return;
            }

            _logger.LogWarning(
                "Initial shell navigation activation failed. content={Content} reason={Reason}",
                ShellNavigationContent.Start,
                "ActivationRejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Initial shell navigation activation threw. content={Content} reason={Reason}",
                ShellNavigationContent.Start,
                ex.GetType().Name);
        }
        finally
        {
            Interlocked.Exchange(ref _activationInFlight, 0);
        }
    }
}
