namespace SalmonEgg.Presentation.Core.Services.Chat;

/// <summary>
/// Describes why a chat service is being replaced so the receiver can decide
/// whether conversation runtime state must be invalidated.
/// </summary>
public enum ServiceReplaceIntent
{
    /// <summary>
    /// The foreground chat page is adopting a new service; runtime states must be reset.
    /// </summary>
    ForegroundOwner,

    /// <summary>
    /// A background pool connection changed; the visible conversation runtime must be preserved.
    /// </summary>
    PoolOnly,

    /// <summary>
    /// Service is being disconnected entirely.
    /// </summary>
    Disconnect
}
