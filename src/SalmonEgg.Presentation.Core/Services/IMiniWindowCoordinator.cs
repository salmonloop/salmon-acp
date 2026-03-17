using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Services;

public interface IMiniWindowCoordinator
{
    Task OpenMiniWindowAsync();

    Task ReturnToMainWindowAsync();
}
