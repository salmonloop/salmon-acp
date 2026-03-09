namespace SalmonEgg.Domain.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";

    public bool IsAnimationEnabled { get; set; } = true;

    public string? LastSelectedServerId { get; set; }

    // General
    public bool LaunchOnStartup { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// Language tag, e.g. "System", "zh-CN", "en-US".
    /// </summary>
    public string Language { get; set; } = "System";

    // Appearance
    /// <summary>
    /// Backdrop preference: "System", "Mica", "Acrylic", "Solid".
    /// </summary>
    public string Backdrop { get; set; } = "System";
}

