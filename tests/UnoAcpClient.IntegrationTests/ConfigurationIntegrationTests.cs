using System;
using System.IO;
using System.Threading.Tasks;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Infrastructure.Storage;
using Xunit;

namespace UnoAcpClient.IntegrationTests
{
    /// <summary>
    /// 配置管理集成测试
    /// 测试配置的保存、加载、加密等完整流程
    /// </summary>
    public class ConfigurationIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly SecureStorage _secureStorage;
        private readonly ConfigurationManager _configManager;

        public ConfigurationIntegrationTests()
        {
            // 创建临时测试目录
            _testDirectory = Path.Combine(Path.GetTempPath(), "UnoAcpClientIntegrationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // 设置测试环境变量
            Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDirectory, EnvironmentVariableTarget.Process);

            _secureStorage = new SecureStorage();
            _configManager = new ConfigurationManager(_secureStorage);
        }

        public void Dispose()
        {
            // 清理测试目录
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        /// <summary>
        /// 测试配置保存和加载的完整流程
        /// </summary>
        [Fact]
        public async Task SaveThenLoadConfiguration_ShouldPreserveAllData()
        {
            // Arrange
            var config = new ServerConfiguration
            {
                Id = "integration-test-001",
                Name = "Integration Test Server",
                ServerUrl = "wss://integration-test.example.com",
                Transport = TransportType.WebSocket,
                HeartbeatInterval = 60,
                ConnectionTimeout = 20,
                Authentication = new AuthenticationConfig
                {
                    Token = "integration-token-123",
                    ApiKey = "integration-api-key-456"
                },
                Proxy = new ProxyConfig
                {
                    Enabled = true,
                    ProxyUrl = "http://proxy.integration.com:8080"
                }
            };

            // Act - 保存配置
            await _configManager.SaveConfigurationAsync(config);

            // Act - 重新加载配置
            var loaded = await _configManager.LoadConfigurationAsync("integration-test-001");

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(config.Id, loaded.Id);
            Assert.Equal(config.Name, loaded.Name);
            Assert.Equal(config.ServerUrl, loaded.ServerUrl);
            Assert.Equal(config.Transport, loaded.Transport);
            Assert.Equal(config.HeartbeatInterval, loaded.HeartbeatInterval);
            Assert.Equal(config.ConnectionTimeout, loaded.ConnectionTimeout);
            Assert.NotNull(loaded.Authentication);
            Assert.Equal(config.Authentication.Token, loaded.Authentication.Token);
            Assert.Equal(config.Authentication.ApiKey, loaded.Authentication.ApiKey);
            Assert.NotNull(loaded.Proxy);
            Assert.Equal(config.Proxy.Enabled, loaded.Proxy.Enabled);
            Assert.Equal(config.Proxy.ProxyUrl, loaded.Proxy.ProxyUrl);
        }

        /// <summary>
        /// 测试多个服务器配置的管理
        /// </summary>
        [Fact]
        public async Task ManageMultipleConfigurations_ShouldPreserveAll()
        {
        // 使用唯一 ID 避免与其他测试冲突
        var uniqueId = Guid.NewGuid().ToString();
        var config1 = new ServerConfiguration
        {
        Id = $"multi-{uniqueId}-1",
        Name = "Server 1",
        ServerUrl = "wss://server1.example.com",
        Transport = TransportType.WebSocket
        };

        var config2 = new ServerConfiguration
        {
        Id = $"multi-{uniqueId}-2",
        Name = "Server 2",
        ServerUrl = "wss://server2.example.com",
        Transport = TransportType.HttpSse
        };

        var config3 = new ServerConfiguration
        {
        Id = $"multi-{uniqueId}-3",
        Name = "Server 3",
        ServerUrl = "wss://server3.example.com",
        Transport = TransportType.WebSocket
        };

        // Act - 保存所有配置
        await _configManager.SaveConfigurationAsync(config1);
        await _configManager.SaveConfigurationAsync(config2);
        await _configManager.SaveConfigurationAsync(config3);

        // Act - 列出所有配置
        var configurations = await _configManager.ListConfigurationsAsync();
        var configList = new System.Collections.Generic.List<ServerConfiguration>(configurations);

        // Assert - 验证包含这 3 个配置（总数可能多于 3 个因为其他测试）
        Assert.Contains(configList, c => c.Id == $"multi-{uniqueId}-1");
        Assert.Contains(configList, c => c.Id == $"multi-{uniqueId}-2");
        Assert.Contains(configList, c => c.Id == $"multi-{uniqueId}-3");
        }

        /// <summary>
        /// 测试配置删除的完整流程
        /// </summary>
        [Fact]
        public async Task DeleteConfiguration_ShouldRemoveCompletely()
        {
            // Arrange
            var config = new ServerConfiguration
            {
                Id = "to-delete-001",
                Name = "To Delete",
                ServerUrl = "wss://delete.example.com",
                Transport = TransportType.WebSocket,
                Authentication = new AuthenticationConfig
                {
                    Token = "delete-token",
                    ApiKey = "delete-key"
                }
            };

            // Act - 保存配置
            await _configManager.SaveConfigurationAsync(config);

            // Verify 配置存在
            var beforeDelete = await _configManager.LoadConfigurationAsync("to-delete-001");
            Assert.NotNull(beforeDelete);

            // Act - 删除配置
            await _configManager.DeleteConfigurationAsync("to-delete-001");

            // Assert - 配置应该完全消失
            var afterDelete = await _configManager.LoadConfigurationAsync("to-delete-001");
            Assert.Null(afterDelete);
        }

        /// <summary>
        /// 测试敏感信息存储和加载
        /// </summary>
        [Fact]
        public async Task SensitiveData_ShouldBeStoredAndLoaded()
        {
            // Arrange
            var config = new ServerConfiguration
            {
                Id = "encrypted-001",
                Name = "Encrypted Config",
                ServerUrl = "wss://encrypted.example.com",
                Transport = TransportType.WebSocket,
                Authentication = new AuthenticationConfig
                {
                    Token = "super-secret-token-12345",
                    ApiKey = "super-secret-api-key-67890"
                }
            };

            // Act - 保存配置
            await _configManager.SaveConfigurationAsync(config);

            // Act - 重新加载配置
            var loaded = await _configManager.LoadConfigurationAsync("encrypted-001");

            // Assert - 验证加载的数据正确
            Assert.NotNull(loaded);
            Assert.NotNull(loaded.Authentication);
            Assert.Equal("super-secret-token-12345", loaded.Authentication.Token);
            Assert.Equal("super-secret-api-key-67890", loaded.Authentication.ApiKey);
        }

        /// <summary>
        /// 测试配置更新流程
        /// </summary>
        [Fact]
        public async Task UpdateConfiguration_ShouldPreserveChanges()
        {
            // Arrange - 创建初始配置
            var config = new ServerConfiguration
            {
                Id = "update-test-001",
                Name = "Original Name",
                ServerUrl = "wss://original.example.com",
                Transport = TransportType.WebSocket,
                HeartbeatInterval = 30
            };

            await _configManager.SaveConfigurationAsync(config);

            // Act - 修改配置
            config.Name = "Updated Name";
            config.ServerUrl = "wss://updated.example.com";
            config.HeartbeatInterval = 60;

            await _configManager.SaveConfigurationAsync(config);

            // Assert - 验证更新后的值
            var loaded = await _configManager.LoadConfigurationAsync("update-test-001");
            Assert.Equal("Updated Name", loaded.Name);
            Assert.Equal("wss://updated.example.com", loaded.ServerUrl);
            Assert.Equal(60, loaded.HeartbeatInterval);
        }

        /// <summary>
        /// 测试损坏配置的处理
        /// </summary>
        [Fact]
        public async Task CorruptedConfiguration_ShouldHandleGracefully()
        {
            // Arrange - 直接写入损坏的数据
            var configId = "corrupted-test";
            await _secureStorage.SaveAsync($"config_{configId}", "{ invalid json }");

            // Act - 尝试加载损坏的配置
            var loaded = await _configManager.LoadConfigurationAsync(configId);

            // Assert - 应该返回 null 或默认配置，不应该崩溃
            Assert.Null(loaded);
        }

        /// <summary>
        /// 测试应用启动时加载最后使用的配置
        /// </summary>
        [Fact]
        public async Task AppStartup_ShouldLoadLastUsedConfiguration()
        {
            // Arrange - 保存一个配置并模拟上次使用的配置
            var config = new ServerConfiguration
            {
                Id = "last-used-001",
                Name = "Last Used Server",
                ServerUrl = "wss://lastused.example.com",
                Transport = TransportType.WebSocket
            };

            await _configManager.SaveConfigurationAsync(config);

            // Act - 模拟应用启动，创建新的配置管理器实例
            var newSecureStorage = new SecureStorage();
            var newConfigManager = new ConfigurationManager(newSecureStorage);

            // Act - 加载配置
            var loaded = await newConfigManager.LoadConfigurationAsync("last-used-001");

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(config.Id, loaded.Id);
            Assert.Equal(config.Name, loaded.Name);
            Assert.Equal(config.ServerUrl, loaded.ServerUrl);
        }
    }
}
