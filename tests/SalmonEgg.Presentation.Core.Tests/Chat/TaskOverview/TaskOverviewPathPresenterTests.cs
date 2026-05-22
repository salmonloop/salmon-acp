using SalmonEgg.Presentation.Core.ViewModels.Chat.TaskOverview;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat.TaskOverview;

public sealed class TaskOverviewPathPresenterTests
{
    [Theory]
    [InlineData("/home/dev/repo/src/App.cs", "App.cs", "/home/dev/repo/src")]
    [InlineData(@"C:\repo\src\App.cs", "App.cs", @"C:\repo\src")]
    [InlineData(@"\\server\share\repo\App.cs", "App.cs", @"\\server\share\repo")]
    [InlineData("/home/dev/repo/", "repo", "/home/dev")]
    public void Present_SplitsRemotePathWithoutDependingOnHostPlatform(
        string path,
        string expectedFileName,
        string expectedDirectoryPath)
    {
        var parts = TaskOverviewPathPresenter.Present(path);

        Assert.Equal(expectedFileName, parts.FileName);
        Assert.Equal(expectedDirectoryPath, parts.DirectoryPath);
    }

    [Fact]
    public void TaskOverviewChangeViewModel_DoesNotExposeUnlocalizedKindDisplayName()
    {
        Assert.Null(typeof(TaskOverviewChangeViewModel).GetProperty("KindDisplayName"));
    }
}
