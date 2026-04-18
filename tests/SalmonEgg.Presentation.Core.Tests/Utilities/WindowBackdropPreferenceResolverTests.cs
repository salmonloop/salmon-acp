using SalmonEgg.Presentation.Utilities;

namespace SalmonEgg.Presentation.Core.Tests.Utilities;

public sealed class WindowBackdropPreferenceResolverTests
{
    [Theory]
    [InlineData("Mica", true, true, WindowBackdropKind.Mica)]
    [InlineData("Mica", false, true, WindowBackdropKind.Acrylic)]
    [InlineData("Mica", false, false, WindowBackdropKind.None)]
    [InlineData("Acrylic", true, true, WindowBackdropKind.Acrylic)]
    [InlineData("Acrylic", true, false, WindowBackdropKind.None)]
    [InlineData("Solid", true, true, WindowBackdropKind.None)]
    [InlineData("System", true, true, WindowBackdropKind.Mica)]
    [InlineData("System", false, true, WindowBackdropKind.Acrylic)]
    [InlineData("System", false, false, WindowBackdropKind.None)]
    [InlineData("  ", true, true, WindowBackdropKind.Mica)]
    [InlineData(null, false, true, WindowBackdropKind.Acrylic)]
    public void Resolve_ReturnsExpectedBackdropKind(
        string? preference,
        bool supportsMica,
        bool supportsAcrylic,
        WindowBackdropKind expected)
    {
        var actual = WindowBackdropPreferenceResolver.Resolve(
            preference,
            supportsMica,
            supportsAcrylic);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_TreatsUnknownPreference_AsSystem()
    {
        var actual = WindowBackdropPreferenceResolver.Resolve(
            "Unexpected",
            supportsMica: false,
            supportsAcrylic: true);

        Assert.Equal(WindowBackdropKind.Acrylic, actual);
    }
}
