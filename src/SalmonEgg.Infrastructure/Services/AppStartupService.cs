using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class AppStartupService : IAppStartupService
{
    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public async Task<bool?> GetLaunchOnStartupAsync()
    {
#if WINDOWS || WINDOWS_UWP
        try
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync("SalmonEggStartup").AsTask().ConfigureAwait(false);
            return task.State == Windows.ApplicationModel.StartupTaskState.Enabled;
        }
        catch
        {
            return null;
        }
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    public async Task<bool> SetLaunchOnStartupAsync(bool enabled)
    {
#if WINDOWS || WINDOWS_UWP
        try
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync("SalmonEggStartup").AsTask().ConfigureAwait(false);
            if (enabled)
            {
                var result = await task.RequestEnableAsync().AsTask().ConfigureAwait(false);
                return result == Windows.ApplicationModel.StartupTaskState.Enabled;
            }

            task.Disable();
            return true;
        }
        catch
        {
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }
}
