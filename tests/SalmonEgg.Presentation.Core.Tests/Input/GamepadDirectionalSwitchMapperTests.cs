using System.Linq;
using SalmonEgg.Presentation.Core.Services.Input;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Input;

public sealed class GamepadDirectionalSwitchMapperTests
{
    [Theory]
    [InlineData(GamepadDirectionalSwitchPosition.Center)]
    public void Apply_CenterDoesNotChangeReading(GamepadDirectionalSwitchPosition position)
    {
        var reading = GamepadDirectionalSwitchMapper.Apply(position, default);

        Assert.Empty(GamepadIntentProcessor.GetActiveIntents(reading));
    }

    [Theory]
    [InlineData(GamepadDirectionalSwitchPosition.Up, GamepadNavigationIntent.MoveUp)]
    [InlineData(GamepadDirectionalSwitchPosition.Right, GamepadNavigationIntent.MoveRight)]
    [InlineData(GamepadDirectionalSwitchPosition.Down, GamepadNavigationIntent.MoveDown)]
    [InlineData(GamepadDirectionalSwitchPosition.Left, GamepadNavigationIntent.MoveLeft)]
    public void Apply_MapsCardinalPositionsToSingleDirection(
        GamepadDirectionalSwitchPosition position,
        GamepadNavigationIntent expected)
    {
        var reading = GamepadDirectionalSwitchMapper.Apply(position, default);

        Assert.Equal([expected], GamepadIntentProcessor.GetActiveIntents(reading));
    }

    [Theory]
    [InlineData(GamepadDirectionalSwitchPosition.UpRight, GamepadNavigationIntent.MoveUp, GamepadNavigationIntent.MoveRight)]
    [InlineData(GamepadDirectionalSwitchPosition.DownRight, GamepadNavigationIntent.MoveDown, GamepadNavigationIntent.MoveRight)]
    [InlineData(GamepadDirectionalSwitchPosition.DownLeft, GamepadNavigationIntent.MoveDown, GamepadNavigationIntent.MoveLeft)]
    [InlineData(GamepadDirectionalSwitchPosition.UpLeft, GamepadNavigationIntent.MoveUp, GamepadNavigationIntent.MoveLeft)]
    public void Apply_MapsDiagonalPositionsToTwoDirections(
        GamepadDirectionalSwitchPosition position,
        GamepadNavigationIntent first,
        GamepadNavigationIntent second)
    {
        var reading = GamepadDirectionalSwitchMapper.Apply(position, default);

        Assert.Equal(
            new[] { first, second }.OrderBy(static intent => intent),
            GamepadIntentProcessor.GetActiveIntents(reading).OrderBy(static intent => intent));
    }
}
