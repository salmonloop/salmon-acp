using System;
using System.IO;

namespace SalmonEgg.Presentation.Core.Tests.Utilities;

public sealed class TranscriptPointerIntentFilterSourceTests
{
    [Fact]
    public void ResolveSourceKind_TreatsMarkdownPresenterAsContentOwnedInteraction()
    {
        var source = LoadRepoFile(
            "SalmonEgg",
            "SalmonEgg",
            "Presentation",
            "Utilities",
            "TranscriptPointerIntentFilter.cs");

        Assert.Contains("MarkdownTextPresenter", source, StringComparison.Ordinal);
        Assert.Contains("TranscriptPointerSourceKind.InteractiveChild", source, StringComparison.Ordinal);
    }

    private static string LoadRepoFile(params string[] segments)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine([root, .. segments]));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SalmonEgg.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root (SalmonEgg.sln) not found.");
    }
}
