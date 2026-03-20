using System.Collections.Generic;
using System.ComponentModel;

namespace SalmonEgg.Presentation.Core.Services.Chat;

/// <summary>
/// Read-only conversation catalog surface for navigation and other consumers.
/// </summary>
public interface IConversationCatalogReadModel : INotifyPropertyChanged
{
    bool IsConversationListLoading { get; }

    int ConversationListVersion { get; }

    IReadOnlyList<ConversationCatalogItem> Snapshot { get; }
}
