using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SalmonEgg.Domain.Models.Content;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Domain.Tests.Protocol;

[TestFixture]
public sealed class SessionPromptTypesTests
{
    [Test]
    public void SessionPromptParams_Prompt_Should_Deserialize_As_ContentBlock_List()
    {
        var json = """
        {
          "sessionId": "test-session",
          "prompt": [
            { "type": "text", "text": "Hello, world!" }
          ]
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionPromptParams>(json);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Prompt, Is.Not.Null);
        Assert.That(parsed.Prompt, Has.Count.EqualTo(1));
        Assert.That(parsed.Prompt![0], Is.TypeOf<TextContentBlock>());
    }

    [Test]
    public void SessionPromptParams_Prompt_Should_Serialize_As_Array()
    {
        // Given: A SessionPromptParams with content blocks
        var sessionParams = new SessionPromptParams
        {
            SessionId = "test-session",
            Prompt = new List<ContentBlock>
            {
                new TextContentBlock { Text = "Hello, world!" }
            }
        };

        // When: Serialize to JSON
        var json = JsonSerializer.Serialize(sessionParams);
        var parsed = JsonDocument.Parse(json);

        // Then: prompt should be an array in JSON
        Assert.That(parsed.RootElement.TryGetProperty("prompt", out var prompt), Is.True);
        Assert.That(prompt.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }
}
