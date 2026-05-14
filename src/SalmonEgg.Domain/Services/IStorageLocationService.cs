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
    Task OpenAsync(AppStorageLocation location);

    Task OpenExistingFolderAsync(string path);
}
