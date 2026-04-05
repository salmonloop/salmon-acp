using System.Text.Json;
using NUnit.Framework;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Domain.Tests.Protocol;

[TestFixture]
public sealed class SessionUpdateTypesTests
{
    [Test]
    public void SessionUpdateParams_Update_RoundTripsAsSessionUpdatePayload()
    {
        var sessionParams = new SessionUpdateParams
        {
            SessionId = "test-session",
            Update = new CurrentModeUpdate { CurrentModeId = "test-mode" }
        };

        var json = JsonSerializer.Serialize(sessionParams);
        var parsed = JsonSerializer.Deserialize<SessionUpdateParams>(json);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.SessionId, Is.EqualTo("test-session"));
        Assert.That(parsed.Update, Is.TypeOf<CurrentModeUpdate>());
    }

    [Test]
    public void SessionUpdateParams_Should_Serialize_With_Update()
    {
        // Given: A SessionUpdateParams with an update
        var sessionParams = new SessionUpdateParams
        {
            SessionId = "test-session",
            Update = new CurrentModeUpdate { CurrentModeId = "test-mode" }
        };

        // When: Serialize to JSON
        var json = JsonSerializer.Serialize(sessionParams);
        var parsed = JsonDocument.Parse(json);

        // Then: update should be present in JSON
        Assert.That(parsed.RootElement.TryGetProperty("update", out var update), Is.True);
        Assert.That(update.ValueKind, Is.EqualTo(JsonValueKind.Object));
    }

    [Test]
    public void ConfigOptionUpdate_ConfigOptions_RoundTrips()
    {
        var update = new ConfigOptionUpdate
        {
            ConfigOptions = [new ConfigOption { Id = "mode", Name = "Mode", Type = "select" }]
        };

        var json = JsonSerializer.Serialize(update);
        var parsed = JsonSerializer.Deserialize<ConfigOptionUpdate>(json);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.ConfigOptions, Is.Not.Null);
        Assert.That(parsed.ConfigOptions!.Count, Is.EqualTo(1));
        Assert.That(parsed.ConfigOptions[0].Id, Is.EqualTo("mode"));
    }
}
