namespace SalmonEgg.Presentation.ViewModels.Chat;

/// <summary>
/// Captures the markdown-related presentation state for a single chat message.
/// </summary>
public sealed record ChatMarkdownPresentationState(ChatMarkdownRenderMode RenderMode, string CopyableCodeBlockText)
{
    /// <summary>
    /// A reusable plain-text projection with no markdown affordances.
    /// </summary>
    public static ChatMarkdownPresentationState PlainStreaming { get; } =
        new(ChatMarkdownRenderMode.PlainStreaming, string.Empty);

    /// <summary>
    /// Gets a value indicating whether the message should render as markdown.
    /// </summary>
    public bool ShouldRenderMarkdown => RenderMode == ChatMarkdownRenderMode.MarkdownReady;

    /// <summary>
    /// Gets a value indicating whether the message should render as plain text.
    /// </summary>
    public bool ShouldRenderPlainText => !ShouldRenderMarkdown;

    /// <summary>
    /// Gets a value indicating whether the message exposes a copyable fenced code block.
    /// </summary>
    public bool HasCopyableCodeBlock => !string.IsNullOrEmpty(CopyableCodeBlockText);

    /// <summary>
    /// Creates a markdown presentation state from the resolved render mode and source text.
    /// </summary>
    /// <param name="renderMode">The resolved markdown render mode.</param>
    /// <param name="text">The source text content.</param>
    /// <returns>A presentation projection for the message.</returns>
    public static ChatMarkdownPresentationState Create(ChatMarkdownRenderMode renderMode, string? text)
    {
        if (renderMode != ChatMarkdownRenderMode.MarkdownReady)
        {
            return PlainStreaming with { RenderMode = renderMode };
        }

        return new(
            renderMode,
            ChatMarkdownCodeBlockExtractor.TryExtractFirstFencedCodeBlock(text) ?? string.Empty);
    }
}
