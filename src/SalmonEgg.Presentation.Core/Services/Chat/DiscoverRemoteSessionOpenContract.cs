namespace SalmonEgg.Presentation.Core.Services.Chat;

public readonly record struct DiscoverRemoteSessionOpenRequest(
    string RemoteSessionId,
    string? RemoteSessionCwd,
    string? ProfileId,
    string? RemoteSessionTitle);

public readonly record struct DiscoverRemoteSessionOpenResult(
    bool Succeeded,
    string? LocalConversationId,
    string? ErrorMessage);
