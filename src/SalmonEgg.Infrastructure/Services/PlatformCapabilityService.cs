using System;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class PlatformCapabilityService : IPlatformCapabilityService
{
    public bool SupportsLaunchOnStartup => OperatingSystem.IsWindows();

    public bool SupportsTray => OperatingSystem.IsWindows();

    public bool SupportsLanguageOverride => OperatingSystem.IsWindows();
}
