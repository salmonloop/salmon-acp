using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IConversationCatalog : INotifyPropertyChanged
{
    bool IsConversationListLoading { get; }

    int ConversationListVersion { get; }

    string[] GetKnownConversationIds();

    Task RestoreAsync(CancellationToken cancellationToken = default);

    void RenameConversation(string conversationId, string newDisplayName);

    void ArchiveConversation(string conversationId);

    void DeleteConversation(string conversationId);
}
