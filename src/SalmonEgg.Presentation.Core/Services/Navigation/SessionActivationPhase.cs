namespace SalmonEgg.Presentation.Core.Services;

public enum SessionActivationPhase
{
    None = 0,
    NavigatingToChatShell = 1,
    SelectingConversation = 2,
    Selected = 3,
    RemoteHydrationPending = 4,
    Hydrated = 5,
    Faulted = 6
}
