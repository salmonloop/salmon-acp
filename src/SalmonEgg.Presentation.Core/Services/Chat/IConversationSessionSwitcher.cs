using System.Threading;
using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IConversationSessionSwitcher
{
    string? CurrentConversationId { get; }

    Task<bool> TrySwitchToSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
