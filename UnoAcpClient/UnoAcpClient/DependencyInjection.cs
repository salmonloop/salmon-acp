```using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using UnoAcpClient.Application.Services;
using UnoAcpClient.Application.Services.Chat;
using UnoAcpClient.Application.UseCases;
using UnoAcpClient.Application.Validators;
using UnoAcpClient.Domain.Interfaces;
using UnoAcpClient.Domain.Interfaces.Transport;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Domain.Services;
using UnoAcpClient.Domain.Services.Security;
using UnoAcpClient.Infrastructure.Client;
using UnoAcpClient.Infrastructure.Logging;
using UnoAcpClient.Infrastructure.Network;
using UnoAcpClient.Infrastructure.Serialization;
using UnoAcpClient.Infrastructure.Storage;
using UnoAcpClient.Infrastructure.Transport;
using UnoAcpClient.Presentation.ViewModels;
using UnoAcpClient.Presentation.ViewModels.Chat;

namespace UnoAcpClient;

/// <summary>
/// 依赖注入容器配置
/// Requirements: 7.5
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 配置所有服务和依赖项
    /// </summary>
    public static IServiceCollection AddUnoAcpClient(this IServiceCollection services)
    {
        ConfigureLogging(services);
        RegisterDomainServices(services);
        RegisterInfrastructureServices(services);
        RegisterApplicationServices(services);
        RegisterViewModels(services);
        return services;
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var appDataPath = GetAppDataPath();
        var logger = LoggingConfiguration.ConfigureLogging(appDataPath);
        Log.Logger = logger;
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });
        services.AddSingleton(logger);
    }

    private static void RegisterDomainServices(IServiceCollection services)
    {
        // ACP 协议服务
        services.AddSingleton<IAcpProtocolService, AcpMessageParser>();

        // 消息解析器和验证器
        services.AddSingleton<IMessageParser, MessageParser>();
        services.AddSingleton<IMessageValidator, MessageValidator>();

        // 能力管理器
        services.AddSingleton<ICapabilityManager, CapabilityManager>();

        // 会话管理器
        services.AddSingleton<ISessionManager, SessionManager>();

        // 路径验证器
        services.AddSingleton<IPathValidator, PathValidator>();

        // 权限管理器
        services.AddSingleton<IPermissionManager, PermissionManager>();

        // 错误日志器
        services.AddSingleton<IErrorLogger, ErrorLogger>();

        // 连接管理器（使用工厂方法支持动态传输选择）
        services.AddSingleton<IConnectionManager>(sp =>
        {
            var protocolService = sp.GetRequiredService<IAcpProtocolService>();
            var logger = sp.GetRequiredService<Serilog.ILogger>();

            ITransport TransportFactory(TransportType type)
            {
                var l = sp.GetRequiredService<Serilog.ILogger>();
                return type switch
                {
                    TransportType.HttpSse => new HttpSseTransport(l),
                    _ => new WebSocketTransport(l)
                };
            }

            return new ConnectionManager(protocolService, logger, TransportFactory);
        });
    }

    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        // 安全存储
        services.AddSingleton<ISecureStorage, SecureStorage>();

        // 配置管理器
        services.AddSingleton<IConfigurationService, ConfigurationManager>();

        // Validator
        services.AddSingleton<IValidator<ServerConfiguration>, ServerConfigurationValidator>();

        // Stdio 传输层
        services.AddSingleton<ITransport>(sp =>
        {
            var logger = sp.GetRequiredService<Serilog.ILogger>();
            return new StdioTransport("agent-command", Array.Empty<string>(), logger);
        });
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // 用例
        services.AddTransient<ConnectToServerUseCase>();
        services.AddTransient<DisconnectUseCase>();
        services.AddTransient<SendMessageUseCase>();

        // 应用服务
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IMessageService, MessageService>();

        // Chat 服务（核心重构部分）
        services.AddSingleton<IChatService>(sp =>
        {
            var transport = sp.GetRequiredService<ITransport>();
            var parser = sp.GetRequiredService<IMessageParser>();
            var validator = sp.GetRequiredService<IMessageValidator>();
            var errorLogger = sp.GetRequiredService<IErrorLogger>();

            var acpClient = new AcpClient(transport, parser, validator, errorLogger);
            return new ChatService(acpClient, errorLogger);
        });
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        // 原有 ViewModel
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ConfigurationEditorViewModel>();

        // 新的 Chat ViewModel（重构后）
        services.AddTransient<ChatViewModel>();
    }

    private static string GetAppDataPath()
    {
#if __ANDROID__
        return Android.App.Application.Context.FilesDir?.AbsolutePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnoAcpClient");
#elif __IOS__
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Application Support", "UnoAcpClient");
#elif __MACOS__
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnoAcpClient");
#elif WINDOWS || WINDOWS_UWP
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnoAcpClient");
#elif __WASM__
        return "/local/UnoAcpClient";
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnoAcpClient");
#endif
    }
}
