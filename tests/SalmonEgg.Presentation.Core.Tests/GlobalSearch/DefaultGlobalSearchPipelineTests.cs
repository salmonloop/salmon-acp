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
