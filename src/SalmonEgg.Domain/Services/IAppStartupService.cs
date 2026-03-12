using System.Threading.Tasks;

namespace SalmonEgg.Domain.Services;

public interface IAppStartupService
{
    bool IsSupported { get; }

    Task<bool?> GetLaunchOnStartupAsync();

    Task<bool> SetLaunchOnStartupAsync(bool enabled);
}
