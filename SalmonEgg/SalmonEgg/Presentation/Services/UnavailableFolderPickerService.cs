using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Services;

public sealed class UnavailableFolderPickerService : IFolderPickerService
{
    public bool IsSupported => false;

    public Task<string?> PickFolderAsync()
        => Task.FromResult<string?>(null);
}
