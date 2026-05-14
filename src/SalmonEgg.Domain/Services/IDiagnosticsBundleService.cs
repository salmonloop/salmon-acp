using System.Threading.Tasks;
using SalmonEgg.Domain.Models.Diagnostics;

namespace SalmonEgg.Domain.Services;

public interface IDiagnosticsBundleService
{
    /// <summary>
    /// Creates a diagnostic zip bundle and returns its absolute path.
    /// </summary>
    Task<DiagnosticsBundleResult> CreateBundleAsync(DiagnosticsSnapshot snapshot);
}

public enum DiagnosticsBundleStatus
{
    Success,
    Unsupported
}

public sealed record DiagnosticsBundleResult(DiagnosticsBundleStatus Status, string? Path)
{
    public static DiagnosticsBundleResult Success(string path) => new(DiagnosticsBundleStatus.Success, path);

    public static DiagnosticsBundleResult Unsupported() => new(DiagnosticsBundleStatus.Unsupported, null);
}
