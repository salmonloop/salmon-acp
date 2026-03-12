using System;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class AppLanguageService : IAppLanguageService
{
    public bool IsSupported => OperatingSystem.IsWindows();

    public Task ApplyLanguageOverrideAsync(string languageTag)
    {
#if WINDOWS || WINDOWS_UWP
        try
        {
            var tag = string.IsNullOrWhiteSpace(languageTag) || string.Equals(languageTag, "System", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : languageTag.Trim();
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = tag;
        }
        catch
        {
        }
#endif
        return Task.CompletedTask;
    }
}
