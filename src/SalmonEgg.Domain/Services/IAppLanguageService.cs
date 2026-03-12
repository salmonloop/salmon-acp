using System.Threading.Tasks;

namespace SalmonEgg.Domain.Services;

public interface IAppLanguageService
{
    bool IsSupported { get; }

    Task ApplyLanguageOverrideAsync(string languageTag);
}
