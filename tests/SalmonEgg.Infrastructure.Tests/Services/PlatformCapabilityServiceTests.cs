using System.Runtime.InteropServices;
using SalmonEgg.Infrastructure.Services;
using Xunit;

namespace SalmonEgg.Infrastructure.Tests.Services;

public sealed class PlatformCapabilityServiceTests
{
    [Fact]
    public void SupportsLocalTerminal_RequiresTransportAndInteractiveSurface()
    {
        var sut = new PlatformCapabilityService();

        Assert.Equal(
            sut.SupportsStdioTransport && sut.SupportsInteractiveTerminalSurface,
            sut.SupportsLocalTerminal);
    }

    [Fact]
    public void SupportsInteractiveTerminalSurface_FollowsWebView2HostAvailability()
    {
        var sut = new PlatformCapabilityService();

        Assert.Equal(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            sut.SupportsInteractiveTerminalSurface);
    }
}
