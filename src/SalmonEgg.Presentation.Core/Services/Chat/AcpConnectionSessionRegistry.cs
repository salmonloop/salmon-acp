using System;
using System.Collections.Generic;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public sealed record AcpConnectionSession(
    string ProfileId,
    AcpChatServiceAdapter Service,
    InitializeResponse InitializeResponse,
    string ConnectionSignature);

public interface IAcpConnectionSessionRegistry
{
    bool TryGetByProfile(string profileId, out AcpConnectionSession session);

    bool TryGetProfileId(IChatService service, out string profileId);

    void Upsert(AcpConnectionSession session);

    bool RemoveByProfile(string profileId);

    bool RemoveByService(IChatService service, out string profileId);
}

public sealed class InMemoryAcpConnectionSessionRegistry : IAcpConnectionSessionRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, AcpConnectionSession> _sessionsByProfile = new(StringComparer.Ordinal);

    public bool TryGetByProfile(string profileId, out AcpConnectionSession session)
    {
        lock (_gate)
        {
            return _sessionsByProfile.TryGetValue(profileId, out session!);
        }
    }

    public bool TryGetProfileId(IChatService service, out string profileId)
    {
        ArgumentNullException.ThrowIfNull(service);
        lock (_gate)
        {
            foreach (var pair in _sessionsByProfile)
            {
                if (ReferenceEquals(pair.Value.Service, service))
                {
                    profileId = pair.Key;
                    return true;
                }
            }
        }

        profileId = string.Empty;
        return false;
    }

    public void Upsert(AcpConnectionSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            _sessionsByProfile[session.ProfileId] = session;
        }
    }

    public bool RemoveByProfile(string profileId)
    {
        lock (_gate)
        {
            return _sessionsByProfile.Remove(profileId);
        }
    }

    public bool RemoveByService(IChatService service, out string profileId)
    {
        ArgumentNullException.ThrowIfNull(service);
        lock (_gate)
        {
            foreach (var pair in _sessionsByProfile)
            {
                if (ReferenceEquals(pair.Value.Service, service))
                {
                    profileId = pair.Key;
                    _sessionsByProfile.Remove(pair.Key);
                    return true;
                }
            }
        }

        profileId = string.Empty;
        return false;
    }
}
