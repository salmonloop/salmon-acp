using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Core.Services;

public interface IShellStartupNavigationService
{
    Task ActivateInitialContentAsync();
}
