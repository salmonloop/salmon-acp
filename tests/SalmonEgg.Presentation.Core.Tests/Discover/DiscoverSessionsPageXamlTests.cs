using System.IO;

namespace SalmonEgg.Presentation.Core.Tests.Discover;

public sealed class DiscoverSessionsPageXamlTests
{
    [Fact]
    public void DiscoverSessionsPage_AffinityPreview_UsesViewModelDrivenBindings()
    {
        // Arrange
        var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Discover\DiscoverSessionsPage.xaml");

        // Assert
        Assert.Contains("Text=\"{x:Bind ProjectAffinityBadgeText, Mode=OneTime}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{x:Bind AffinityStatusText, Mode=OneTime}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{x:Bind NeedsUserAttention, Mode=OneTime, Converter={StaticResource BoolToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoverSessionsPage_AffinityPreview_DoesNotHardcodeResolverFallbackText()
    {
        // Arrange
        var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Discover\DiscoverSessionsPage.xaml");

        // Assert
        Assert.DoesNotContain("Needs mapping", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Unclassified", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoverSessionsPage_ResponsivePaneVisibility_IsViewModelDriven()
    {
        // Arrange
        var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Discover\DiscoverSessionsPage.xaml");

        // Assert
        Assert.Contains("Visibility=\"{x:Bind ViewModel.ShowProfilesPane, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{x:Bind ViewModel.ShowDetailsPane, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=\"800\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoverSessionsPage_AdaptiveLayout_UsesNativeStates_AndAvoidsOpacityHack()
    {
        // Arrange
        var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Discover\DiscoverSessionsPage.xaml");

        // Assert
        Assert.Contains("AdaptiveTrigger", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Uid=\"DiscoverSessionsBackButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ActionBtn.Opacity", xaml, StringComparison.Ordinal);
    }

    private static string LoadFile(string relativePath)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(root, relativePath));
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
