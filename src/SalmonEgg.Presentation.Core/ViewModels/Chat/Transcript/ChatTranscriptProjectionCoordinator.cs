using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SalmonEgg.Domain.Interfaces.Storage;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Domain.Models.ConversationPreview;

namespace SalmonEgg.Presentation.ViewModels.Chat.Transcript;

internal sealed class ChatTranscriptProjectionCoordinator
{
    private readonly IConversationPreviewStore _previewStore;
    private readonly object _previewSnapshotSync = new();
    private string? _lastSavedPreviewConversationId;
    private IImmutableList<ConversationMessageSnapshot>? _lastSavedPreviewTranscript;

    public ChatTranscriptProjectionCoordinator(IConversationPreviewStore previewStore)
    {
        _previewStore = previewStore ?? throw new ArgumentNullException(nameof(previewStore));
    }

    public void ApplyProjection(
        ChatTranscriptProjectionContext context,
        string? conversationId,
        IImmutableList<ConversationMessageSnapshot> transcript,
        bool sessionChanged)
    {
        ArgumentNullException.ThrowIfNull(context);

        ApplyTranscript(context, conversationId, transcript);
    }

    public ConversationPreviewSnapshot? BuildPreviewSnapshot(
        string? conversationId,
        IImmutableList<ConversationMessageSnapshot> transcript,
        bool isHydrating)
    {
        if (isHydrating || transcript.Count == 0 || string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        var previewEntries = transcript
            .Select(m => new PreviewEntry(
                m.IsOutgoing ? "user" : "assistant",
                m.TextContent ?? string.Empty,
                m.Timestamp))
            .ToArray();

        return new ConversationPreviewSnapshot(
            conversationId,
            previewEntries,
            DateTimeOffset.Now);
    }

    public ConversationPreviewSnapshot? PreparePreviewSnapshotSave(
        string? conversationId,
        IImmutableList<ConversationMessageSnapshot> transcript,
        bool isHydrating)
    {
        var snapshot = BuildPreviewSnapshot(conversationId, transcript, isHydrating);
        if (snapshot is null)
        {
            return null;
        }

        lock (_previewSnapshotSync)
        {
            var previewTranscriptChanged =
                !string.Equals(_lastSavedPreviewConversationId, conversationId, StringComparison.Ordinal)
                || !ReferenceEquals(_lastSavedPreviewTranscript, transcript);
            if (!previewTranscriptChanged)
            {
                return null;
            }

            _lastSavedPreviewConversationId = conversationId;
            _lastSavedPreviewTranscript = transcript;
        }

        return snapshot;
    }

    public void SavePreviewSnapshot(ConversationPreviewSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _ = _previewStore.SaveAsync(snapshot);
    }

    public void UpdatePreviewSnapshot(
        string? conversationId,
        IImmutableList<ConversationMessageSnapshot> transcript,
        bool isHydrating)
    {
        var snapshot = PreparePreviewSnapshotSave(conversationId, transcript, isHydrating);
        if (snapshot is null)
        {
            return;
        }

        SavePreviewSnapshot(snapshot);
    }

    private static void ApplyTranscript(
        ChatTranscriptProjectionContext context,
        string? conversationId,
        IImmutableList<ConversationMessageSnapshot> transcript)
    {
        var history = context.GetMessageHistory();
        var previousCount = history.Count;
        history.Reset(
            conversationId,
            transcript ?? ImmutableList<ConversationMessageSnapshot>.Empty,
            context.FromSnapshot,
            context.MatchesSnapshot);
        var transcriptOwnerChanged = context.UpdateVisibleTranscriptConversationId(conversationId, history.Count > 0);
        if (previousCount != history.Count || transcriptOwnerChanged)
        {
            context.RaiseTranscriptStateChanged();
        }
    }
}
