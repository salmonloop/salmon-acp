using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Tests.Storage;

internal sealed class FailingAppFileStore : IAppFileStore
{
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => throw new IOException("read failed");

    public Task<string?> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => throw new IOException("read failed");

    public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
        => throw new IOException("write failed");

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        => throw new IOException("delete failed");

    public async IAsyncEnumerable<string> EnumerateFilesAsync(
        string directory,
        string searchPattern,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw new IOException("enumerate failed");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
