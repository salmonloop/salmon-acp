using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Tests.Localization;
using SalmonEgg.Presentation.Core.Services.Search;
using SalmonEgg.Presentation.Models.Search;

namespace SalmonEgg.Presentation.Core.Tests.GlobalSearch;

public sealed class DefaultGlobalSearchPipelineTests
{
    [Fact]
    public async Task SearchAsync_SettingsAndCommandsUseLocalizedResources()
    {
        var pipeline = new DefaultGlobalSearchPipeline(new TestCoreStringLocalizer());

        var result = await pipeline.SearchAsync(
            "主题",
            new GlobalSearchSourceSnapshot(
                ImmutableArray<GlobalSearchSessionSource>.Empty,
                ImmutableArray<GlobalSearchProjectSource>.Empty),
            CancellationToken.None);

        var items = result.Groups.SelectMany(group => group.Items).ToArray();

        Assert.Contains(
            items,
            item => item.Kind == SearchResultKind.Setting
                && item.Id == "General"
                && item.Title == "常规"
                && item.Subtitle == "主题、语言、启动选项");
        Assert.Contains(
            items,
            item => item.Kind == SearchResultKind.Command
                && item.Id == "toggle_theme"
                && item.Title == "切换主题"
                && item.Subtitle == "在亮色、暗色和系统主题间切换");
    }

    [Fact]
    public async Task SearchAsync_DoesNotReturnUnsupportedAnimationCommand()
    {
        var pipeline = new DefaultGlobalSearchPipeline(new TestCoreStringLocalizer());

        var result = await pipeline.SearchAsync(
            "toggle_anim",
            new GlobalSearchSourceSnapshot(
                ImmutableArray<GlobalSearchSessionSource>.Empty,
                ImmutableArray<GlobalSearchProjectSource>.Empty),
            CancellationToken.None);

        Assert.DoesNotContain(
            result.Groups.SelectMany(group => group.Items),
            item => item.Kind == SearchResultKind.Command && item.Id == "toggle_anim");
    }
}
