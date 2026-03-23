using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Presentation.Core.Mvux.Chat;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IWorkspaceWriter
{
    void Enqueue(ChatState state, bool scheduleSave);

    Task FlushAsync(CancellationToken cancellationToken = default);
}

public sealed class WorkspaceWriter : IWorkspaceWriter, IDisposable
{
    private const int DefaultThrottleMilliseconds = 500;

    private readonly ChatConversationWorkspace _workspace;
    private readonly SynchronizationContext _syncContext;
    private readonly TimeSpan _throttleWindow;
    private CancellationTokenSource? _flushCts;
    private PendingWrite? _pending;
    private DateTime _lastFlushAt;
    private bool _disposed;

    public WorkspaceWriter(
        ChatConversationWorkspace workspace,
        TimeSpan? throttleWindow = null,
        SynchronizationContext? syncContext = null)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _syncContext = syncContext ?? SynchronizationContext.Current ?? new SynchronizationContext();
        _throttleWindow = throttleWindow ?? TimeSpan.FromMilliseconds(DefaultThrottleMilliseconds);
        _lastFlushAt = DateTime.MinValue;
    }

    public void Enqueue(ChatState state, bool scheduleSave)
    {
        ThrowIfDisposed();
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var pending = CreatePendingWrite(state, scheduleSave);
        if (pending is null)
        {
            return;
        }

        if (_pending?.ScheduleSave == true)
        {
            pending.ScheduleSave = true;
        }

        _pending = pending;

        var delay = ComputeDelay();
        if (delay <= TimeSpan.Zero)
        {
            _ = FlushAsync();
            return;
        }

        ScheduleDelayedFlush(delay);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var pending = _pending;
        if (pending is null)
        {
            return;
        }

        _pending = null;
        CancelScheduledFlush();
        _lastFlushAt = DateTime.UtcNow;

        await PostToContextAsync(() =>
        {
            _workspace.UpsertConversationSnapshot(pending.Snapshot);
            if (pending.ScheduleSave)
            {
                _workspace.ScheduleSave();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pending = null;
        CancelScheduledFlush();
    }

    private PendingWrite? CreatePendingWrite(ChatState state, bool scheduleSave)
    {
        var conversationId = state.HydratedConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        if (state.Transcript is null && state.PlanEntries is null)
        {
            return null;
        }

        var transcript = (state.Transcript ?? ImmutableList<ConversationMessageSnapshot>.Empty)
            .Where(static message => !IsThinkingPlaceholder(message))
            .Select(CloneMessageSnapshot)
            .ToArray();
        var planEntries = (state.PlanEntries ?? ImmutableList<ConversationPlanEntrySnapshot>.Empty)
            .Select(ClonePlanEntrySnapshot)
            .OfType<ConversationPlanEntrySnapshot>()
            .ToArray();
        var existingSnapshot = _workspace.GetConversationSnapshot(conversationId);
        var lastUpdatedAt = existingSnapshot != null
            && SnapshotContentMatches(existingSnapshot, transcript, planEntries, state.ShowPlanPanel, state.PlanTitle)
            ? existingSnapshot.LastUpdatedAt
            : DateTime.UtcNow;

        var snapshot = new ConversationWorkspaceSnapshot(
            conversationId,
            transcript,
            planEntries,
            state.ShowPlanPanel,
            state.PlanTitle,
            default,
            lastUpdatedAt);

        return new PendingWrite(snapshot, scheduleSave);
    }

    private TimeSpan ComputeDelay()
    {
        if (_lastFlushAt == DateTime.MinValue)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTime.UtcNow - _lastFlushAt;
        var remaining = _throttleWindow - elapsed;
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void ScheduleDelayedFlush(TimeSpan delay)
    {
        CancelScheduledFlush();

        _flushCts = new CancellationTokenSource();
        var token = _flushCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
                await FlushAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }, token);
    }

    private void CancelScheduledFlush()
    {
        _flushCts?.Cancel();
        _flushCts?.Dispose();
        _flushCts = null;
    }

    private Task PostToContextAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (SynchronizationContext.Current == _syncContext)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _syncContext.Post(_ =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);

        return tcs.Task;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static bool IsThinkingPlaceholder(ConversationMessageSnapshot message)
        => string.Equals(message.ContentType, "thinking", StringComparison.OrdinalIgnoreCase);

    private static bool SnapshotContentMatches(
        ConversationWorkspaceSnapshot existingSnapshot,
        IReadOnlyList<ConversationMessageSnapshot> transcript,
        IReadOnlyList<ConversationPlanEntrySnapshot> planEntries,
        bool showPlanPanel,
        string? planTitle)
    {
        return existingSnapshot.ShowPlanPanel == showPlanPanel
            && string.Equals(existingSnapshot.PlanTitle, planTitle, StringComparison.Ordinal)
            && MessageSequencesEqual(existingSnapshot.Transcript, transcript)
            && PlanSequencesEqual(existingSnapshot.Plan, planEntries);
    }

    private static bool MessageSequencesEqual(
        IReadOnlyList<ConversationMessageSnapshot> left,
        IReadOnlyList<ConversationMessageSnapshot> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!MessageEquals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool PlanSequencesEqual(
        IReadOnlyList<ConversationPlanEntrySnapshot> left,
        IReadOnlyList<ConversationPlanEntrySnapshot> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!PlanEntryEquals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MessageEquals(ConversationMessageSnapshot left, ConversationMessageSnapshot right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.Ordinal)
            && left.Timestamp == right.Timestamp
            && left.IsOutgoing == right.IsOutgoing
            && string.Equals(left.ContentType, right.ContentType, StringComparison.Ordinal)
            && string.Equals(left.Title, right.Title, StringComparison.Ordinal)
            && string.Equals(left.TextContent, right.TextContent, StringComparison.Ordinal)
            && string.Equals(left.ImageData, right.ImageData, StringComparison.Ordinal)
            && string.Equals(left.ImageMimeType, right.ImageMimeType, StringComparison.Ordinal)
            && string.Equals(left.AudioData, right.AudioData, StringComparison.Ordinal)
            && string.Equals(left.AudioMimeType, right.AudioMimeType, StringComparison.Ordinal)
            && string.Equals(left.ToolCallId, right.ToolCallId, StringComparison.Ordinal)
            && left.ToolCallKind == right.ToolCallKind
            && left.ToolCallStatus == right.ToolCallStatus
            && string.Equals(left.ToolCallJson, right.ToolCallJson, StringComparison.Ordinal)
            && string.Equals(left.ModeId, right.ModeId, StringComparison.Ordinal)
            && PlanEntryEquals(left.PlanEntry, right.PlanEntry);
    }

    private static bool PlanEntryEquals(ConversationPlanEntrySnapshot? left, ConversationPlanEntrySnapshot? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return string.Equals(left.Content, right.Content, StringComparison.Ordinal)
            && left.Status == right.Status
            && left.Priority == right.Priority;
    }

    private static ConversationMessageSnapshot CloneMessageSnapshot(ConversationMessageSnapshot snapshot)
        => new()
        {
            Id = snapshot.Id,
            Timestamp = snapshot.Timestamp,
            IsOutgoing = snapshot.IsOutgoing,
            ContentType = snapshot.ContentType,
            Title = snapshot.Title,
            TextContent = snapshot.TextContent,
            ImageData = snapshot.ImageData,
            ImageMimeType = snapshot.ImageMimeType,
            AudioData = snapshot.AudioData,
            AudioMimeType = snapshot.AudioMimeType,
            ToolCallId = snapshot.ToolCallId,
            ToolCallKind = snapshot.ToolCallKind,
            ToolCallStatus = snapshot.ToolCallStatus,
            ToolCallJson = snapshot.ToolCallJson,
            PlanEntry = ClonePlanEntrySnapshot(snapshot.PlanEntry),
            ModeId = snapshot.ModeId
        };

    private static ConversationPlanEntrySnapshot? ClonePlanEntrySnapshot(ConversationPlanEntrySnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return new ConversationPlanEntrySnapshot
        {
            Content = snapshot.Content,
            Status = snapshot.Status,
            Priority = snapshot.Priority
        };
    }

    private sealed class PendingWrite
    {
        public PendingWrite(ConversationWorkspaceSnapshot snapshot, bool scheduleSave)
        {
            Snapshot = snapshot;
            ScheduleSave = scheduleSave;
        }

        public ConversationWorkspaceSnapshot Snapshot { get; }

        public bool ScheduleSave { get; set; }
    }
}
