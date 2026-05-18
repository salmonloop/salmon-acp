using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Services;

public interface IFolderPickerService
{
    bool IsSupported { get; }

    Task<string?> PickFolderAsync();
}
