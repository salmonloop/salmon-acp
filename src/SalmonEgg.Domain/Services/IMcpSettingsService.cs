using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Domain.Models.Mcp;

namespace SalmonEgg.Domain.Services;

public interface IMcpSettingsService
{
    Task<McpSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(McpSettings settings, CancellationToken cancellationToken = default);
}
