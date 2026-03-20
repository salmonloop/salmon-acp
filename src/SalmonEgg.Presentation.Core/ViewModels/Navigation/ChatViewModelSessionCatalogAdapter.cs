using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Services.Chat;

namespace SalmonEgg.Presentation.ViewModels.Navigation;

public sealed class ChatViewModelSessionCatalogAdapter : IChatSessionCatalog
{
    private readonly IConversationCatalog _conversationCatalog;

    public ChatViewModelSessionCatalogAdapter(IConversationCatalog conversationCatalog)
    {
        _conversationCatalog = conversationCatalog ?? throw new ArgumentNullException(nameof(conversationCatalog));
    }

    public bool IsConversationListLoading => _conversationCatalog.IsConversationListLoading;

    public int ConversationListVersion => _conversationCatalog.ConversationListVersion;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => _conversationCatalog.PropertyChanged += value;
        remove => _conversationCatalog.PropertyChanged -= value;
    }

    public string[] GetKnownConversationIds() => _conversationCatalog.GetKnownConversationIds();

    public Task RestoreAsync(CancellationToken cancellationToken = default)
        => _conversationCatalog.RestoreAsync(cancellationToken);

    public void RenameConversation(string conversationId, string newName)
        => _conversationCatalog.RenameConversation(conversationId, newName);

    public void ArchiveConversation(string conversationId)
        => _conversationCatalog.ArchiveConversation(conversationId);

    public void DeleteConversation(string conversationId)
        => _conversationCatalog.DeleteConversation(conversationId);
}
