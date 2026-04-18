namespace SalmonEgg.Presentation.Utilities;

public static class WindowBackdropPreferenceResolver
{
    public static WindowBackdropKind Resolve(
        string? preference,
        bool supportsMica,
        bool supportsAcrylic)
    {
        var normalized = string.IsNullOrWhiteSpace(preference)
            ? "System"
            : preference.Trim();

        return normalized switch
        {
            "Mica" => supportsMica
                ? WindowBackdropKind.Mica
                : supportsAcrylic
                    ? WindowBackdropKind.Acrylic
                    : WindowBackdropKind.None,
            "Acrylic" => supportsAcrylic
                ? WindowBackdropKind.Acrylic
                : WindowBackdropKind.None,
            "Solid" => WindowBackdropKind.None,
            _ => supportsMica
                ? WindowBackdropKind.Mica
                : supportsAcrylic
                    ? WindowBackdropKind.Acrylic
                    : WindowBackdropKind.None
        };
    }
}
