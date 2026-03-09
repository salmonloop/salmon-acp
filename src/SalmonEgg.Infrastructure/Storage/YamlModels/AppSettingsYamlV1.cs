using System;

namespace SalmonEgg.Infrastructure.Storage.YamlModels;

internal sealed class AppSettingsYamlV1
{
    public int SchemaVersion { get; set; } = 1;

    public string UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");

    public string Theme { get; set; } = "System";

    public bool IsAnimationEnabled { get; set; } = true;

    public string LastSelectedServerId { get; set; } = string.Empty;

    // General
    public bool LaunchOnStartup { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public string Language { get; set; } = "System";

    // Appearance
    public string Backdrop { get; set; } = "System";
}

