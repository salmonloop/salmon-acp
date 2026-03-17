using System;
using System.Collections.Immutable;

namespace SalmonEgg.Presentation.Core.Mvux.Chat;

public sealed partial record ChatMessage(
    string Id,
    DateTimeOffset Timestamp,
    bool IsOutgoing,
    string? Content = null,
    IImmutableList<ChatContentPart>? Parts = null)
{
    public ChatMessage MergeDelta(string deltaText)
    {
        var parts = Parts ?? ImmutableList<ChatContentPart>.Empty;

        if (parts.Count > 0 && parts[^1] is TextPart lastTextPart)
        {
            var updatedPart = lastTextPart with { Text = lastTextPart.Text + deltaText };
            return this with { Parts = parts.SetItem(parts.Count - 1, updatedPart) };
        }
        else
        {
            return this with { Parts = parts.Add(new TextPart(deltaText)) };
        }
    }
}
