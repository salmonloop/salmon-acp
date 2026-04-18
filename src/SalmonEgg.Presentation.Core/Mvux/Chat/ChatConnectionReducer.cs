using System;

namespace SalmonEgg.Presentation.Core.Mvux.Chat;

public static class ChatConnectionReducer
{
    public static ChatConnectionState Reduce(ChatConnectionState? state, ChatConnectionAction action)
    {
        var current = state ?? ChatConnectionState.Empty;
        var nextGeneration = checked(current.Generation + 1);

        return action switch
        {
            SetConnectionPhaseAction setPhase => current with
            {
                Phase = setPhase.Phase,
                Error = setPhase.Error,
                CommittedProfileId = setPhase.Phase switch
                {
                    ConnectionPhase.Connected => current.SelectedProfileId,
                    ConnectionPhase.Disconnected or ConnectionPhase.Error => null,
                    _ => current.CommittedProfileId
                },
                Generation = nextGeneration
            },
            SetSelectedProfileAction setProfile => current with
            {
                SelectedProfileId = setProfile.ProfileId,
                CommittedProfileId =
                    string.Equals(current.CommittedProfileId, setProfile.ProfileId, StringComparison.Ordinal)
                        ? current.CommittedProfileId
                        : null,
                Generation = nextGeneration
            },
            SetConnectionAuthenticationStateAction setAuth => current with
            {
                IsAuthenticationRequired = setAuth.IsRequired,
                AuthenticationHintMessage = setAuth.HintMessage,
                Generation = nextGeneration
            },
            ResetConnectionStateAction => ChatConnectionState.Empty with
            {
                Generation = nextGeneration
            },
            _ => current with { Generation = nextGeneration }
        };
    }
}
