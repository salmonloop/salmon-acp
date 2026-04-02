using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Services.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public sealed class AcpConnectionEvictionOptionsLoaderTests
{
    [Fact]
    public void Load_UsesAppSettingsWhenEnvMissing()
    {
        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.LoadAsync())
            .ReturnsAsync(new AppSettings
            {
                AcpEnableConnectionEviction = true,
                AcpConnectionIdleTtlMinutes = 12,
                AcpMaxWarmProfiles = 4,
                AcpMaxPinnedProfiles = 2
            });

        var options = AcpConnectionEvictionOptionsLoader.Load(
            settingsService.Object,
            Mock.Of<ILogger>());

        Assert.True(options.EnablePolicyEviction);
        Assert.Equal(TimeSpan.FromMinutes(12), options.IdleTtl);
        Assert.Equal(4, options.MaxWarmProfiles);
        Assert.Equal(2, options.MaxPinnedProfiles);
    }

    [Fact]
    public void Load_EnvOverridesAppSettings()
    {
        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.LoadAsync())
            .ReturnsAsync(new AppSettings
            {
                AcpEnableConnectionEviction = false,
                AcpConnectionIdleTtlMinutes = 20,
                AcpMaxWarmProfiles = 6,
                AcpMaxPinnedProfiles = 3
            });

        Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_ENABLED", "true");
        Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_IDLE_TTL_MINUTES", "9");
        Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_MAX_WARM_PROFILES", "2");
        Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_MAX_PINNED_PROFILES", "1");
        try
        {
            var options = AcpConnectionEvictionOptionsLoader.Load(
                settingsService.Object,
                Mock.Of<ILogger>());

            Assert.True(options.EnablePolicyEviction);
            Assert.Equal(TimeSpan.FromMinutes(9), options.IdleTtl);
            Assert.Equal(2, options.MaxWarmProfiles);
            Assert.Equal(1, options.MaxPinnedProfiles);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_ENABLED", null);
            Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_IDLE_TTL_MINUTES", null);
            Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_MAX_WARM_PROFILES", null);
            Environment.SetEnvironmentVariable("SALMONEGG_ACP_EVICTION_MAX_PINNED_PROFILES", null);
        }
    }
}

