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

    [Test]
    public void Deserialize_ConfigOptionUpdate_Works()
    {
        var json = """
        {
          "sessionId": "s1",
          "update": {
            "sessionUpdate": "config_option_update",
            "configOptions": [
              {
                "id": "mode",
                "name": "Mode",
                "category": "mode",
                "type": "select",
                "currentValue": "agent",
                "options": [
                  { "value": "agent", "name": "Agent" }
                ]
              }
            ]
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Update, Is.TypeOf<ConfigOptionUpdate>());

        var update = (ConfigOptionUpdate)parsed.Update!;
        Assert.That(update.ConfigOptions, Is.Not.Null.And.Not.Empty);
        Assert.That(update.ConfigOptions![0].Id, Is.EqualTo("mode"));
        Assert.That(update.ConfigOptions[0].CurrentValue, Is.EqualTo("agent"));
    }

    [Test]
    public void Deserialize_SessionInfoUpdate_Works()
    {
        var json = """
        {
          "sessionId": "s-info",
          "update": {
            "sessionUpdate": "session_info_update",
            "title": "New Title",
            "updatedAt": "2026-03-22T19:00:00Z"
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Update, Is.TypeOf<SessionInfoUpdate>());

        var update = (SessionInfoUpdate)parsed.Update!;
        Assert.That(update.Title, Is.EqualTo("New Title"));
        Assert.That(update.UpdatedAt, Is.EqualTo("2026-03-22T19:00:00Z"));
    }

    [Test]
    public void Deserialize_CurrentModeUpdate_LegacyModeId_Works()
    {
        var json = """
        {
          "sessionId": "s1",
          "update": {
            "sessionUpdate": "current_mode_update",
            "modeId": "legacy-mode"
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Update, Is.TypeOf<CurrentModeUpdate>());

        var update = (CurrentModeUpdate)parsed.Update!;
        Assert.That(update.NormalizedModeId, Is.EqualTo("legacy-mode"));
    }
}
