using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlaUI.Core.Definitions;
using FlaUI.Core.AutomationElements;

namespace SalmonEgg.GuiTests.Windows;

public sealed partial class RealUserConfigSmokeTests
{
    [SkippableFact]
    public void SelectRemoteBoundSession_FromStart_DoesNotSnapBackToStartSelection()
    {
        GuiTestGate.RequireEnabled();

        var candidates = RealUserConfigProbe.LoadReplayBackedCandidates();
        Skip.If(candidates.Count == 0, "No real remote-bound conversations with recent session/load + session/update evidence were found in the current SalmonEgg app data.");

        using var session = WindowsGuiAppSession.LaunchFresh();

        var startItem = session.FindByAutomationId("MainNav.Start", TimeSpan.FromSeconds(10));
        session.ActivateElement(startItem);
        Thread.Sleep(500);

        var candidate = candidates
            .FirstOrDefault(item => session.TryFindByAutomationId(SessionAutomationId(item.ConversationId), TimeSpan.FromSeconds(1)) is not null);
        Skip.If(candidate is null, $"No replay-backed conversation is currently visible in the left navigation. Candidates: {string.Join(", ", candidates.Select(c => c.ConversationId))}");

        var sessionItem = session.FindByAutomationId(SessionAutomationId(candidate.ConversationId), TimeSpan.FromSeconds(10));
        session.ActivateElement(sessionItem);

        var timeline = new List<string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        var sawSessionSelected = false;
        var snappedBackToStart = false;

        while (DateTime.UtcNow < deadline)
        {
            var sessionSelected = session.TryGetIsSelected(SessionAutomationId(candidate.ConversationId)) == true;
            var startSelected = session.TryGetIsSelected("MainNav.Start") == true;
            var headerVisible = session.TryFindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromMilliseconds(100)) is not null;
            var overlayVisible = session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromMilliseconds(100)) is not null;

            timeline.Add(
                $"{DateTime.UtcNow:HH:mm:ss.fff} sessionSelected={sessionSelected} startSelected={startSelected} header={headerVisible} overlay={overlayVisible}");

            if (sessionSelected)
            {
                sawSessionSelected = true;
            }

            if (sawSessionSelected && startSelected && !sessionSelected)
            {
                snappedBackToStart = true;
                break;
            }

            if (sawSessionSelected && (overlayVisible || headerVisible))
            {
                break;
            }

            Thread.Sleep(150);
        }

        Assert.False(
            snappedBackToStart,
            $"After selecting remote-bound conversation {candidate.ConversationId}, left navigation snapped back to Start while activation was still unfolding.{Environment.NewLine}{string.Join(Environment.NewLine, timeline)}");
    }

    [SkippableFact]
    public void SelectRemoteBoundSession_FromStart_KeepsLoadingVisible_UntilRemoteReplayProjects()
    {
        GuiTestGate.RequireEnabled();

        var candidates = RealUserConfigProbe.LoadReplayBackedCandidates();
        Skip.If(candidates.Count == 0, "No real remote-bound conversations with recent session/load + session/update evidence were found in the current SalmonEgg app data.");

        using var session = WindowsGuiAppSession.LaunchFresh();

        var startItem = session.FindByAutomationId("MainNav.Start", TimeSpan.FromSeconds(10));
        session.ActivateElement(startItem);
        Thread.Sleep(500);

        var candidate = candidates
            .FirstOrDefault(item => session.TryFindByAutomationId(SessionAutomationId(item.ConversationId), TimeSpan.FromSeconds(1)) is not null);
        Skip.If(candidate is null, $"No replay-backed conversation is currently visible in the left navigation. Candidates: {string.Join(", ", candidates.Select(c => c.ConversationId))}");

        var sessionItem = session.FindByAutomationId(SessionAutomationId(candidate.ConversationId), TimeSpan.FromSeconds(10));

        session.ActivateElement(sessionItem);

        var timeline = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        var sawOverlay = false;
        var sawTranscriptVisible = false;
        var sawPrematureDismissal = false;
        string? prematureDismissalCapturePath = null;
        long? overlayDismissedAtMs = null;

        while (DateTime.UtcNow < deadline)
        {
            var overlayVisible = session.TryFindByAutomationId("ChatView.LoadingOverlay", TimeSpan.FromMilliseconds(100)) is not null;
            var header = session.TryFindByAutomationId("ChatView.CurrentSessionNameButton", TimeSpan.FromMilliseconds(100));
            var messagesList = session.TryFindByAutomationId("ChatView.MessagesList", TimeSpan.FromMilliseconds(100));
            var visibleTranscriptTextCount = CountVisibleTranscriptText(messagesList);
            var selected = session.TryGetIsSelected(SessionAutomationId(candidate.ConversationId));

            timeline.Add(
                $"{stopwatch.ElapsedMilliseconds,5}ms overlay={overlayVisible} header={(header is not null)} selected={selected} visibleText={visibleTranscriptTextCount}");

            if (overlayVisible)
            {
                sawOverlay = true;
            }

            if (visibleTranscriptTextCount > 0)
            {
                sawTranscriptVisible = true;
            }

            if (sawOverlay && !overlayVisible && overlayDismissedAtMs is null)
            {
                overlayDismissedAtMs = stopwatch.ElapsedMilliseconds;
            }

            if (overlayDismissedAtMs is not null
                && !sawTranscriptVisible
                && stopwatch.ElapsedMilliseconds - overlayDismissedAtMs.Value > 500)
            {
                var captureRoot = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests");
                Directory.CreateDirectory(captureRoot);
                prematureDismissalCapturePath = Path.Combine(
                    captureRoot,
                    $"remote-overlay-premature-{candidate.ConversationId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
                session.MainWindow.CaptureToFile(prematureDismissalCapturePath);
                sawPrematureDismissal = true;
                break;
            }

            if (sawOverlay && sawTranscriptVisible && !overlayVisible)
            {
                break;
            }

            Thread.Sleep(200);
        }

        var failureDetails = string.Join(Environment.NewLine, timeline);

        Assert.True(
            sawOverlay,
            $"The real-config navigation path never surfaced ChatView.LoadingOverlay for remote-bound conversation {candidate.ConversationId}.{Environment.NewLine}{failureDetails}");

        Assert.False(
            sawPrematureDismissal,
            $"Loading overlay disappeared before any transcript content became visible for conversation {candidate.ConversationId}.{Environment.NewLine}Capture: {prematureDismissalCapturePath ?? "<none>"}{Environment.NewLine}{failureDetails}");

        Assert.True(
            sawTranscriptVisible,
            $"Real-config smoke did not observe any transcript content for conversation {candidate.ConversationId} within the timeout window.{Environment.NewLine}{failureDetails}");
    }

    private static int CountVisibleTranscriptText(AutomationElement? messagesList)
    {
        if (messagesList is null)
        {
            return 0;
        }

        return messagesList
            .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Count(element => !TryGetIsOffscreen(element) && !string.IsNullOrWhiteSpace(element.Name));
    }

    private static string SessionAutomationId(string conversationId)
        => $"MainNav.Session.{conversationId}";

    private sealed record RealReplayCandidate(
        string ConversationId,
        string RemoteSessionId,
        int LocalMessageCount,
        DateTimeOffset LastUpdatedAtUtc);

    private static class RealUserConfigProbe
    {
        public static IReadOnlyList<RealReplayCandidate> LoadReplayBackedCandidates()
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SalmonEgg");
            var appYamlPath = Path.Combine(appDataRoot, "config", "app.yaml");
            var conversationsPath = Path.Combine(appDataRoot, "conversations", "conversations.v1.json");
            var logsRoot = Path.Combine(appDataRoot, "logs");

            if (!File.Exists(appYamlPath) || !File.Exists(conversationsPath) || !Directory.Exists(logsRoot))
            {
                return [];
            }

            var selectedProfileId = ReadYamlScalar(appYamlPath, "last_selected_server_id");
            if (string.IsNullOrWhiteSpace(selectedProfileId))
            {
                return [];
            }

            var replayedRemoteIds = ReadReplayBackedRemoteSessionIds(logsRoot);
            if (replayedRemoteIds.Count == 0)
            {
                return [];
            }

            using var document = JsonDocument.Parse(File.ReadAllText(conversationsPath));
            if (!document.RootElement.TryGetProperty("conversations", out var conversationsElement)
                || conversationsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var candidates = new List<RealReplayCandidate>();
            foreach (var conversationElement in conversationsElement.EnumerateArray())
            {
                var boundProfileId = ReadString(conversationElement, "boundProfileId");
                var remoteSessionId = ReadString(conversationElement, "remoteSessionId");
                if (!string.Equals(boundProfileId, selectedProfileId, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(remoteSessionId)
                    || !replayedRemoteIds.Contains(remoteSessionId))
                {
                    continue;
                }

                var conversationId = ReadString(conversationElement, "conversationId");
                if (string.IsNullOrWhiteSpace(conversationId))
                {
                    continue;
                }

                var messageCount = conversationElement.TryGetProperty("messages", out var messagesElement)
                    && messagesElement.ValueKind == JsonValueKind.Array
                    ? messagesElement.GetArrayLength()
                    : 0;

                var lastUpdatedAt = conversationElement.TryGetProperty("lastUpdatedAt", out var lastUpdatedElement)
                    && lastUpdatedElement.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(lastUpdatedElement.GetString(), out var parsedLastUpdatedAt)
                        ? parsedLastUpdatedAt
                        : DateTimeOffset.MinValue;

                candidates.Add(new RealReplayCandidate(
                    conversationId,
                    remoteSessionId,
                    messageCount,
                    lastUpdatedAt));
            }

            return candidates
                .OrderBy(candidate => candidate.LocalMessageCount)
                .ThenByDescending(candidate => candidate.LastUpdatedAtUtc)
                .ToArray();
        }

        private static HashSet<string> ReadReplayBackedRemoteSessionIds(string logsRoot)
        {
            var loadedSessionIds = new HashSet<string>(StringComparer.Ordinal);
            var updatedSessionIds = new HashSet<string>(StringComparer.Ordinal);
            var logFiles = Directory.EnumerateFiles(logsRoot, "app-*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(2);

            foreach (var logFile in logFiles)
            {
                foreach (var line in ReadLinesAllowSharedRead(logFile))
                {
                    if (line.Contains("\"method\":\"session/load\"", StringComparison.Ordinal)
                        && TryExtractSessionId(line, out var loadedSessionId))
                    {
                        loadedSessionIds.Add(loadedSessionId);
                    }

                    if (line.Contains("\"method\":\"session/update\"", StringComparison.Ordinal)
                        && TryExtractSessionId(line, out var updatedSessionId))
                    {
                        updatedSessionIds.Add(updatedSessionId);
                    }
                }
            }

            loadedSessionIds.IntersectWith(updatedSessionIds);
            return loadedSessionIds;
        }

        private static IEnumerable<string> ReadLinesAllowSharedRead(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line)
            {
                yield return line;
            }
        }

        private static string? ReadYamlScalar(string yamlPath, string key)
        {
            foreach (var line in File.ReadLines(yamlPath))
            {
                var prefix = key + ":";
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                return line[prefix.Length..].Trim();
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : null;
        }

        private static bool TryExtractSessionId(string line, out string sessionId)
        {
            var match = SessionIdRegex().Match(line);
            if (match.Success)
            {
                sessionId = match.Groups[1].Value;
                return true;
            }

            sessionId = string.Empty;
            return false;
        }
    }

    [GeneratedRegex("\\\"sessionId\\\":\\\"([^\\\"]+)\\\"", RegexOptions.Compiled)]
    private static partial Regex SessionIdRegex();

    private static bool TryGetIsOffscreen(AutomationElement element)
    {
        try
        {
            return element.IsOffscreen;
        }
        catch
        {
            return false;
        }
    }
}
