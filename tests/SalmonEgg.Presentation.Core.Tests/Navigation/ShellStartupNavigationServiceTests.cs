using System.Threading.Tasks;
using Moq;
using SalmonEgg.Presentation.Core.Services;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Navigation;

public sealed class ShellStartupNavigationServiceTests
{
    [Fact]
    public async Task ActivateInitialContentAsync_ActivatesStartThroughNavigationCoordinator()
    {
        var coordinator = new Mock<INavigationCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(x => x.ActivateStartAsync(null))
            .ReturnsAsync(true);
        var service = new ShellStartupNavigationService(coordinator.Object);

        await service.ActivateInitialContentAsync();

        coordinator.Verify(x => x.ActivateStartAsync(null), Times.Once);
        coordinator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ActivateInitialContentAsync_DoesNotRepeatAfterSuccessfulActivation()
    {
        var coordinator = new Mock<INavigationCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(x => x.ActivateStartAsync(null))
            .ReturnsAsync(true);
        var service = new ShellStartupNavigationService(coordinator.Object);

        await service.ActivateInitialContentAsync();
        await service.ActivateInitialContentAsync();

        coordinator.Verify(x => x.ActivateStartAsync(null), Times.Once);
        coordinator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ActivateInitialContentAsync_RetriesAfterRejectedActivation()
    {
        var coordinator = new Mock<INavigationCoordinator>(MockBehavior.Strict);
        coordinator
            .SetupSequence(x => x.ActivateStartAsync(null))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        var service = new ShellStartupNavigationService(coordinator.Object);

        await service.ActivateInitialContentAsync();
        await service.ActivateInitialContentAsync();

        coordinator.Verify(x => x.ActivateStartAsync(null), Times.Exactly(2));
        coordinator.VerifyNoOtherCalls();
    }
}
