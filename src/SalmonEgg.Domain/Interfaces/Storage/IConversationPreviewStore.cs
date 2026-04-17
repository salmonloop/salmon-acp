using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Domain.Models.ConversationPreview;

namespace SalmonEgg.Domain.Interfaces.Storage;

public interface IConversationPreviewStore
{
    Task<ConversationPreviewSnapshot?> LoadAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SaveAsync(ConversationPreviewSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteAsync(string conversationId, CancellationToken cancellationToken = default);
}
