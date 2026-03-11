using System.Text.Json;
using NUnit.Framework;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Domain.Tests.Protocol;

[TestFixture]
public sealed class SessionUpdatePolymorphismTests
{
    [Test]
    public void Deserialize_CurrentModeUpdate_Works()
    {
        var json = """
        {
          "sessionId": "s1",
          "update": {
            "sessionUpdate": "current_mode_update",
            "currentModeId": "mode_123",
            "title": "Claude Code"
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.SessionId, Is.EqualTo("s1"));
        Assert.That(parsed.Update, Is.TypeOf<CurrentModeUpdate>());

        var update = (CurrentModeUpdate)parsed.Update!;
        Assert.That(update.CurrentModeId, Is.EqualTo("mode_123"));
        Assert.That(update.Title, Is.EqualTo("Claude Code"));
    }

    [Test]
    public void Deserialize_ConfigOptionsUpdate_Works()
    {
        var json = """
        {
          "sessionId": "s1",
          "update": {
            "sessionUpdate": "config_options_update",
            "configOptions": []
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Update, Is.TypeOf<ConfigUpdateUpdate>());
    }
}

