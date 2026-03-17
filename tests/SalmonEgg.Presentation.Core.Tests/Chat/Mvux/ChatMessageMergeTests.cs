using System;
using System.Collections.Immutable;
using System.Linq;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Mvux;

public class ChatMessageMergeTests
{
    [Fact]
    public void GivenAssistantMessageWithText_WhenAppendingDelta_ThenTextIsAppendedToLastPart()
    {
        // Arrange
        var messageId = "msg-1";
        var initialText = "Hello";
        var deltaText = " world!";

        var initialMessage = new ChatMessage(
            Id: messageId,
            Timestamp: DateTimeOffset.Now,
            IsOutgoing: false,
            Parts: ImmutableList.Create<ChatContentPart>(new TextPart(initialText))
        );

        // Act
        // Note: MergeDelta is expected to be implemented in ChatMessage or a helper
        var updatedMessage = initialMessage.MergeDelta(deltaText);

        // Assert
        Assert.NotNull(updatedMessage);
        Assert.Equal(messageId, updatedMessage.Id);
        var textPart = Assert.IsType<TextPart>(updatedMessage.Parts?.Single());
        Assert.Equal("Hello world!", textPart.Text);
    }

    [Fact]
    public void GivenAssistantMessageWithoutParts_WhenAppendingDelta_ThenNewTextPartIsCreated()
    {
        // Arrange
        var initialMessage = new ChatMessage(
            Id: "msg-1",
            Timestamp: DateTimeOffset.Now,
            IsOutgoing: false,
            Parts: null
        );

        // Act
        var updatedMessage = initialMessage.MergeDelta("New content");

        // Assert
        var textPart = Assert.IsType<TextPart>(updatedMessage.Parts?.Single());
        Assert.Equal("New content", textPart.Text);
    }
}
