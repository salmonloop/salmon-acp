using System.Threading.Tasks;

namespace SalmonEgg.Domain.Services;

public enum AppStorageLocation
{
    AppData,
    Cache,
    Logs,
    Exports
}

public interface IStorageLocationService
{
    Task<bool> OpenAsync(AppStorageLocation location);

    Task<bool> OpenExistingFolderAsync(string path);
}
