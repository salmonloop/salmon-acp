using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IAcpConnectionPoolManager
{
    Task<AcpConnectionSessionCleanupResult> CleanupBeforeApplyAsync(
        IChatService? activeService,
        string? selectedProfileId,
        CancellationToken cancellationToken = default);

    bool TryGetReusableSession(
        string? selectedProfileId,
        AcpConnectionReuseKey reuseKey,
        out AcpConnectionSession session);

    void RecordSession(
        string profileId,
        AcpChatServiceAdapter service,
        InitializeResponse initializeResponse,
        AcpConnectionReuseKey reuseKey);

    bool RemoveByService(IChatService service, out string profileId);
}

public sealed class AcpConnectionPoolManager : IAcpConnectionPoolManager
{
    private readonly IAcpConnectionSessionRegistry _sessionRegistry;
    private readonly IAcpConnectionSessionCleaner _sessionCleaner;
    private readonly ILogger<AcpConnectionPoolManager> _logger;
    private long _cleanupCount;
    private long _cacheHits;
    private long _cacheMisses;
    private long _sessionUpserts;

    public AcpConnectionPoolManager(
        IAcpConnectionSessionRegistry sessionRegistry,
        IAcpConnectionSessionCleaner sessionCleaner,
        ILogger<AcpConnectionPoolManager> logger)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _sessionCleaner = sessionCleaner ?? throw new ArgumentNullException(nameof(sessionCleaner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AcpConnectionSessionCleanupResult> CleanupBeforeApplyAsync(
        IChatService? activeService,
        string? selectedProfileId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionCleaner
            .CleanupStaleAsync(
                activeService,
                isPinned: IsSoftPinnedSession,
                isHardPinned: session => IsHardPinnedSession(session, selectedProfileId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var cleanupCount = Interlocked.Increment(ref _cleanupCount);
        if (result.RemovedCount > 0 || result.DisposeFailureCount > 0)
        {
            _logger.LogInformation(
                "ACP connection pool cleanup applied. cleanupCount={CleanupCount} removedCount={RemovedCount} disposeFailureCount={DisposeFailureCount}",
                cleanupCount,
                result.RemovedCount,
                result.DisposeFailureCount);
        }

        return result;
    }

    public bool TryGetReusableSession(
        string? selectedProfileId,
        AcpConnectionReuseKey reuseKey,
        out AcpConnectionSession session)
    {
        session = default!;
        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        if (!_sessionRegistry.TryGetByProfile(selectedProfileId!, out var cachedSession)
            || cachedSession.ConnectionReuseKey != reuseKey
            || !cachedSession.Service.IsConnected
            || !cachedSession.Service.IsInitialized)
        {
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        _sessionRegistry.Touch(selectedProfileId!);
        session = cachedSession;
        var hits = Interlocked.Increment(ref _cacheHits);
        _logger.LogDebug(
            "ACP connection pool hit. profileId={ProfileId} totalHits={TotalHits} totalMisses={TotalMisses}",
            selectedProfileId,
            hits,
            Volatile.Read(ref _cacheMisses));
        return true;
    }

    public void RecordSession(
        string profileId,
        AcpChatServiceAdapter service,
        InitializeResponse initializeResponse,
        AcpConnectionReuseKey reuseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(initializeResponse);

        _sessionRegistry.Upsert(new AcpConnectionSession(
            profileId,
            service,
            initializeResponse,
            reuseKey)
        {
            LastUsedUtc = DateTime.UtcNow
        });

        var upserts = Interlocked.Increment(ref _sessionUpserts);
        _logger.LogDebug(
            "ACP connection pool upsert. profileId={ProfileId} totalUpserts={TotalUpserts}",
            profileId,
            upserts);
    }

    public bool RemoveByService(IChatService service, out string profileId)
        => _sessionRegistry.RemoveByService(service, out profileId);

    private static bool IsSoftPinnedSession(AcpConnectionSession session)
        => session.InitializeResponse.AgentCapabilities?.LoadSession != true;

    private static bool IsHardPinnedSession(AcpConnectionSession session, string? selectedProfileId)
        => !string.IsNullOrWhiteSpace(selectedProfileId)
           && string.Equals(session.ProfileId, selectedProfileId, StringComparison.Ordinal);
}

