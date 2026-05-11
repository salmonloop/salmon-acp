namespace SalmonEgg.Presentation.ViewModels.Chat;

/// <summary>
/// Defines the expansion contract for tool call inline details.
/// </summary>
public static class ToolCallPillExpansionPolicy
{
    /// <summary>
    /// Resolves the first expansion state before the user manually toggles details.
    /// </summary>
    /// <param name="isCompleted">Whether the tool call has reached a completed lifecycle state.</param>
    /// <returns><see langword="true" /> when details should initially be expanded.</returns>
    public static bool ResolveDefaultExpanded(bool isCompleted)
        => !isCompleted;

    /// <summary>
    /// Resolves whether inline details should be visible for the current user expansion state.
    /// </summary>
    /// <param name="hasInlineContent">Whether the pill has detail content to show.</param>
    /// <param name="isExpanded">Whether the pill is currently expanded.</param>
    /// <returns><see langword="true" /> when inline details should be visible.</returns>
    public static bool ShouldShowInlineContent(bool hasInlineContent, bool isExpanded)
        => hasInlineContent && isExpanded;
}
