using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SalmonEgg.Domain.Models.Diagnostics;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class DiagnosticsBundleService : IDiagnosticsBundleService
{
    private readonly IAppDataService _paths;
    private readonly IPlatformCapabilityService _capabilities;

    public DiagnosticsBundleService(
        IAppDataService paths,
        IPlatformCapabilityService capabilities)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public async Task<DiagnosticsBundleResult> CreateBundleAsync(DiagnosticsSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        if (!_capabilities.SupportsLocalFileExport)
        {
            return DiagnosticsBundleResult.Unsupported();
        }

        Directory.CreateDirectory(_paths.ExportsDirectoryPath);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var zipPath = Path.Combine(_paths.ExportsDirectoryPath, $"diagnostics-{timestamp}.zip");

        var tempDir = Path.Combine(_paths.ExportsDirectoryPath, $"diagnostics-{timestamp}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var snapshotPath = Path.Combine(tempDir, "snapshot.json");
            var json = JsonSerializer.Serialize(snapshot, DiagnosticsJsonContext.Default.DiagnosticsSnapshot);
            await File.WriteAllTextAsync(snapshotPath, json).ConfigureAwait(false);

            CopyIfExists(Path.Combine(_paths.AppDataRootPath, "boot.log"), Path.Combine(tempDir, "boot.log"));
            CopyDirectoryIfExists(_paths.LogsDirectoryPath, Path.Combine(tempDir, "logs"));
            CopyDirectoryIfExists(_paths.ConfigRootPath, Path.Combine(tempDir, "config"));
            CopyDirectoryIfExists(_paths.CacheRootPath, Path.Combine(tempDir, "cache"));

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            return DiagnosticsBundleResult.Success(zipPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private static void CopyIfExists(string source, string destination)
    {
        try
        {
            if (!File.Exists(source))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }
        catch
        {
        }
    }

    private static void CopyDirectoryIfExists(string sourceDir, string destinationDir)
    {
        try
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            Directory.CreateDirectory(destinationDir);
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var dest = Path.Combine(destinationDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, overwrite: true);
            }
        }
        catch
        {
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DiagnosticsSnapshot))]
internal partial class DiagnosticsJsonContext : JsonSerializerContext
{
}
