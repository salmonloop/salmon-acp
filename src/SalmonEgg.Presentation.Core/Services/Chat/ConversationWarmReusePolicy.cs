using System;
using SalmonEgg.Presentation.Core.Mvux.Chat;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public static class ConversationWarmReusePolicy
{
    public static bool CanReuseRemoteWarmConversation(
        ConversationRuntimeSlice? runtimeState,
        ConversationBindingSlice? binding,
        string? currentConnectionInstanceId)
    {
        if (runtimeState is not { Phase: ConversationRuntimePhase.Warm } hydratedRuntime)
        {
            return false;
        }

        if (binding is null
            || string.IsNullOrWhiteSpace(binding.RemoteSessionId)
            || string.IsNullOrWhiteSpace(currentConnectionInstanceId))
        {
            return false;
        }

        return string.Equals(hydratedRuntime.RemoteSessionId, binding.RemoteSessionId, StringComparison.Ordinal)
            && string.Equals(hydratedRuntime.ProfileId, binding.ProfileId, StringComparison.Ordinal)
            && string.Equals(hydratedRuntime.ConnectionInstanceId, currentConnectionInstanceId, StringComparison.Ordinal);
    }
}
