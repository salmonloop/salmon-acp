using System;
using System.Threading.Tasks;

namespace SalmonEgg.Presentation.Core.Services;

public interface INavigationSelectionHost
{
    void SelectStart();

    void SelectSettings();

    void SelectSession(string sessionId);

    void ToggleProjectExpanded(string projectId);

    void RegisterSessionActivationHandler(Func<string, string?, Task> sessionActivationHandler);
}
