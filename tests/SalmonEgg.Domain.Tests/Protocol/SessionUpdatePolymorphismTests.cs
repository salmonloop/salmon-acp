using System.Text.Json;
using NUnit.Framework;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Infrastructure.Serialization;

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

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new MessageParser().Options);

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

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, CreateJsonOptions());

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

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, CreateJsonOptions());

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

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new MessageParser().Options);

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

    [Test]
    public void SessionInfoUpdate_DoesNotExpose_LegacySessionMetadataProperties()
    {
        Assert.That(typeof(SessionInfoUpdate).GetProperty("Cwd"), Is.Null);
        Assert.That(typeof(SessionInfoUpdate).GetProperty("SessionId"), Is.Null);
    }

    [Test]
    public void Deserialize_ToolCallStatusUpdate_WithExtendedSchemaFields_Works()
    {
        var json = """
        {
          "sessionId": "s1",
          "update": {
            "sessionUpdate": "tool_call_update",
            "toolCallId": "call-1",
            "title": "Switch mode",
            "kind": "switch_mode",
            "status": "completed",
            "rawInput": { "targetMode": "plan" },
            "rawOutput": { "applied": true }
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Update, Is.TypeOf<ToolCallStatusUpdate>());

        var update = (ToolCallStatusUpdate)parsed.Update!;
        Assert.That(update.ToolCallId, Is.EqualTo("call-1"));
        Assert.That(update.Title, Is.EqualTo("Switch mode"));
        Assert.That(update.Kind, Is.EqualTo(Domain.Models.Tool.ToolCallKind.SwitchMode));
        Assert.That(update.Status, Is.EqualTo(Domain.Models.Tool.ToolCallStatus.Completed));
        Assert.That(update.RawInput.HasValue, Is.True);
        Assert.That(update.RawOutput.HasValue, Is.True);
        var rawInput = update.RawInput.GetValueOrDefault();
        var rawOutput = update.RawOutput.GetValueOrDefault();
        Assert.That(rawInput.GetProperty("targetMode").GetString(), Is.EqualTo("plan"));
        Assert.That(rawOutput.GetProperty("applied").GetBoolean(), Is.True);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonPropertyNameEnumConverterFactory());
        return options;
    }
}
