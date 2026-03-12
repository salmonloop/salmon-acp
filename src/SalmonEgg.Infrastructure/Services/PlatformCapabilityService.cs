using System;
using System.Runtime.InteropServices;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class PlatformCapabilityService : IPlatformCapabilityService
{
    public bool SupportsLaunchOnStartup => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public bool SupportsTray => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public bool SupportsLanguageOverride => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
