using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Localization;

public sealed class CoreStringResourceTests
{
    [Theory]
    [InlineData("Platform_ExternalOpenUnsupported")]
    [InlineData("Platform_LocalFileExportUnsupported")]
    public void PlatformMessages_ArePresentInAllCoreStringResources(string key)
    {
        foreach (var relativePath in CoreStringResourcePaths)
        {
            var document = XDocument.Load(Path.Combine(FindRepoRoot(), NormalizeRelativePath(relativePath)));
            var exists = document
                .Descendants("data")
                .Any(element => string.Equals((string?)element.Attribute("name"), key, StringComparison.Ordinal));

            Assert.True(exists, $"{key} must exist in {relativePath}.");
        }
    }

    private static readonly string[] CoreStringResourcePaths =
    [
        @"src\SalmonEgg.Presentation.Core\Resources\CoreStrings.resx",
        @"src\SalmonEgg.Presentation.Core\Resources\CoreStrings.en.resx",
        @"src\SalmonEgg.Presentation.Core\Resources\CoreStrings.en-US.resx"
    ];

    private static string NormalizeRelativePath(string relativePath)
        => relativePath.Replace('\\', Path.DirectorySeparatorChar);

    private static string FindRepoRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "SalmonEgg.sln")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }
}
