using System;
using System.IO;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class StorageLocationService : IStorageLocationService
{
    private readonly IAppDataService _paths;
    private readonly IPlatformCapabilityService _capabilities;
    private readonly IPlatformShellService _shell;

    public StorageLocationService(
        IAppDataService paths,
        IPlatformCapabilityService capabilities,
        IPlatformShellService shell)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    public async Task<bool> OpenAsync(AppStorageLocation location)
    {
        if (!_capabilities.SupportsExternalFileOpen)
        {
            return false;
        }

        var path = ResolvePath(location);
        Directory.CreateDirectory(path);
        return await _shell.OpenFolderAsync(path).ConfigureAwait(false);
    }

    public Task<bool> OpenExistingFolderAsync(string path)
    {
        if (!_capabilities.SupportsExternalFileOpen || string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        if (!Directory.Exists(path))
        {
            return Task.FromResult(false);
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
