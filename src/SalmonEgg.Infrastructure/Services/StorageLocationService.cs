using System;
using System.IO;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class StorageLocationService : IStorageLocationService
{
    private readonly IAppDataService _paths;
    private readonly IPlatformShellService _shell;

    public StorageLocationService(
        IAppDataService paths,
        IPlatformShellService shell)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    public async Task OpenAsync(AppStorageLocation location)
    {
        var path = ResolvePath(location);
        Directory.CreateDirectory(path);
        await _shell.OpenFolderAsync(path).ConfigureAwait(false);
    }

    public Task OpenExistingFolderAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        if (!Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        return _shell.OpenFolderAsync(path);
    }

    private string ResolvePath(AppStorageLocation location)
    {
        return location switch
        {
            AppStorageLocation.AppData => _paths.AppDataRootPath,
            AppStorageLocation.Cache => _paths.CacheRootPath,
            AppStorageLocation.Logs => _paths.LogsDirectoryPath,
            AppStorageLocation.Exports => _paths.ExportsDirectoryPath,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
    }
}
