using SalmonEgg.Presentation.Core.Services;

namespace SalmonEgg.Presentation.Core.Tests.Services;

public sealed class OpenSourceAcknowledgementsManifestParserTests
{
    [Fact]
    public void Parse_ReturnsAcknowledgementsFromManifestRows()
    {
        const string manifest =
            "Alpha.Package\t1.2.3\tMIT\thttps://example.test/alpha\n"
            + "Beta.Package\tSDK-managed\t\t";

        var acknowledgements = OpenSourceAcknowledgementsManifestParser.Parse(manifest);

        Assert.Collection(
            acknowledgements,
            first =>
            {
                Assert.Equal("Alpha.Package", first.Name);
                Assert.Equal("1.2.3", first.Version);
                Assert.Equal("MIT", first.License);
                Assert.Equal("https://example.test/alpha", first.SourceUrl);
            },
            second =>
            {
                Assert.Equal("Beta.Package", second.Name);
                Assert.Equal("SDK-managed", second.Version);
                Assert.Equal(string.Empty, second.License);
                Assert.Equal(string.Empty, second.SourceUrl);
            });
    }

    [Fact]
    public void Parse_SkipsRowsWithoutPackageName()
    {
        const string manifest =
            "\n"
            + "\t1.0\tMIT\thttps://example.test/nameless\n"
            + "  Gamma.Package  \t  4.5.6  ";

        var acknowledgements = OpenSourceAcknowledgementsManifestParser.Parse(manifest);

        var acknowledgement = Assert.Single(acknowledgements);
        Assert.Equal("Gamma.Package", acknowledgement.Name);
        Assert.Equal("4.5.6", acknowledgement.Version);
        Assert.Equal(string.Empty, acknowledgement.License);
        Assert.Equal(string.Empty, acknowledgement.SourceUrl);
    }
}
