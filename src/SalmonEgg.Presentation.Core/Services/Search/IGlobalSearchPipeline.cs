using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Models.Search;

namespace SalmonEgg.Presentation.Core.Services.Search;

public interface IGlobalSearchPipeline
{
    Task<GlobalSearchSnapshot> SearchAsync(
        string query,
        GlobalSearchSourceSnapshot source,
        CancellationToken cancellationToken);
}
