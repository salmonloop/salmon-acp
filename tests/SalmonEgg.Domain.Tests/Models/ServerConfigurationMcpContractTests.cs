using System.Reflection;
using NUnit.Framework;
using SalmonEgg.Domain.Models;

namespace SalmonEgg.Domain.Tests.Models;

public sealed class ServerConfigurationMcpContractTests
{
    [Test]
    public void ServerConfiguration_ProfileModel_Should_NotOwnMcpServers()
    {
        var mcpServersProperty = typeof(ServerConfiguration).GetProperty(
            "McpServers",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.That(mcpServersProperty, Is.Null);
    }
}
