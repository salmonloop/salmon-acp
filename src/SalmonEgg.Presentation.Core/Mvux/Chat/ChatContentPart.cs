namespace SalmonEgg.Presentation.Core.Mvux.Chat;

/// <summary>
/// Base record for all types of message content (text, tool calls, images, etc.)
/// </summary>
public abstract record ChatContentPart;

/// <summary>
/// Represents simple text content.
/// </summary>
public sealed record TextPart(string Text) : ChatContentPart;
