using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SalmonEgg.Domain.Models.Mcp;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Domain.Tests.Protocol;

[TestFixture]
public sealed class SessionNewTypesTests
{
    [Test]
    public void SessionNewParams_McpServers_Should_Serialize_With_TypeDiscriminator()
    {
        var sessionParams = new SessionNewParams
        {
            Cwd = "/home/user/project",
            McpServers =
            [
                new StdioMcpServer("test-server", "node", ["server.js"])
            ]
        };

        var json = JsonSerializer.Serialize(sessionParams);
        var parsed = JsonDocument.Parse(json);

        Assert.That(parsed.RootElement.TryGetProperty("mcpServers", out var mcpServers), Is.True);
        Assert.That(mcpServers.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(mcpServers[0].GetProperty("type").GetString(), Is.EqualTo("stdio"));
    }

    [Test]
    public void SessionNewParams_McpServers_Should_Serialize_As_Array()
    {
        // Given: A SessionNewParams with MCP servers
        var sessionParams = new SessionNewParams
        {
            Cwd = "/home/user/project",
            McpServers = new List<McpServer>
            {
                new StdioMcpServer("test-server", "node", new List<string> { "server.js" })
            }
        };

        // When: Serialize to JSON
        var json = JsonSerializer.Serialize(sessionParams);
        var parsed = JsonDocument.Parse(json);

        // Then: mcpServers should be an array in JSON
        Assert.That(parsed.RootElement.TryGetProperty("mcpServers", out var mcpServers), Is.True);
        Assert.That(mcpServers.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public void SessionNewParams_McpServers_Should_NotBe_Object()
    {
        // Given: A SessionNewParams with MCP servers
        var sessionParams = new SessionNewParams
        {
            Cwd = "/home/user/project",
            McpServers = new List<McpServer>()
        };

        // When: Serialize to JSON
        var json = JsonSerializer.Serialize(sessionParams);

        // Then: JSON should not contain "object" representation
        Assert.That(json, Does.Not.Contain("\"mcpServers\":{}"));
        Assert.That(json, Does.Contain("\"mcpServers\":[]"));
    }

    [Test]
    public void SessionNewResponse_Modes_Should_Deserialize_Standard_State_Object()
    {
        var json = """
        {
          "sessionId": "session-1",
          "modes": {
            "currentModeId": "default",
            "availableModes": [
              {
                "id": "default",
                "name": "Default",
                "description": "General work"
              }
            ]
          }
        }
        """;

        var response = JsonSerializer.Deserialize<SessionNewResponse>(json);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Modes, Is.Not.Null);
        Assert.That(response.Modes!.CurrentModeId, Is.EqualTo("default"));
        Assert.That(response.Modes.AvailableModes, Has.Count.EqualTo(1));
        Assert.That(response.Modes.AvailableModes[0].Id, Is.EqualTo("default"));
    }

    [Test]
    public void SessionNewResponse_Modes_Should_Reject_Legacy_Array()
    {
        var json = """
        {
          "sessionId": "session-1",
          "modes": [
            {
              "id": "default",
              "name": "Default"
            }
          ]
        }
        """;

        Assert.That(
            () => JsonSerializer.Deserialize<SessionNewResponse>(json),
            Throws.TypeOf<JsonException>());
    }
}
