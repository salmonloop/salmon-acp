using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Core.Services.Chat;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Navigation;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.ViewModels.Start;

public sealed partial class StartViewModel : ObservableObject
{
    private readonly AppPreferencesViewModel _preferences;
    private readonly MainNavigationViewModel _nav;
    private readonly IChatLaunchWorkflow _chatLaunchWorkflow;
    private readonly ILogger<StartViewModel> _logger;

    public ChatViewModel Chat { get; }

    private bool _isStarting;

    public bool IsStarting
    {
        get => _isStarting;
        set => SetProperty(ref _isStarting, value);
    }

    public IAsyncRelayCommand StartSessionAndSendCommand { get; }

    public System.Collections.ObjectModel.ObservableCollection<QuickSuggestionViewModel> Suggestions { get; } = new();

    public IRelayCommand<QuickSuggestionViewModel> ExecuteSuggestionCommand { get; }

    public StartViewModel(
        ChatViewModel chatViewModel,
        ISessionManager sessionManager,
        AppPreferencesViewModel preferences,
        INavigationCoordinator navigationCoordinator,
        MainNavigationViewModel nav,
        ILogger<StartViewModel> logger,
        IChatLaunchWorkflow? chatLaunchWorkflow = null,
        IChatConnectionStore? chatConnectionStore = null)
    {
        Chat = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        ArgumentNullException.ThrowIfNull(sessionManager);
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        ArgumentNullException.ThrowIfNull(navigationCoordinator);
        _nav = nav ?? throw new ArgumentNullException(nameof(nav));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatLaunchWorkflow = chatLaunchWorkflow ?? new ChatLaunchWorkflow(
            new ChatLaunchWorkflowChatFacadeAdapter(
                Chat,
                chatConnectionStore ?? throw new ArgumentNullException(nameof(chatConnectionStore))),
            sessionManager,
            _preferences,
            navigationCoordinator,
            ResolveDefaultCwd);

        StartSessionAndSendCommand = new AsyncRelayCommand(StartSessionAndSendAsync, () => !IsStarting);
        ExecuteSuggestionCommand = new RelayCommand<QuickSuggestionViewModel>(ExecuteSuggestion);

        InitializeSuggestions();
    }

    private void InitializeSuggestions()
    {
        Suggestions.Add(new QuickSuggestionViewModel("\uE943", "分析代码库", "深入理解项目架构与逻辑", "请帮我分析一下当前代码库的架构和核心逻辑。", ExecuteSuggestionCommand));
        Suggestions.Add(new QuickSuggestionViewModel("\uE762", "推荐开发任务", "明确接下来该做什么", "根据当前进度，推荐几个接下来可以进行的开发任务或优化点。", ExecuteSuggestionCommand));
        Suggestions.Add(new QuickSuggestionViewModel("\uEBE8", "解决最近报错", "提交错误日志让我看看", "我刚才遇到了一些报错，请帮我分析并解决它们。", ExecuteSuggestionCommand));
    }

    private void ExecuteSuggestion(QuickSuggestionViewModel? suggestion)
    {
        if (suggestion == null) return;
        Chat.CurrentPrompt = suggestion.Prompt;
        StartSessionAndSendCommand.Execute(null);
    }

    private async Task StartSessionAndSendAsync()
    {
        var promptText = (Chat.CurrentPrompt ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(promptText))
        {
            return;
        }

        IsStarting = true;
        StartSessionAndSendCommand.NotifyCanExecuteChanged();
        try
        {
            await _chatLaunchWorkflow.StartSessionAndSendAsync(promptText).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Start session failed");
        }
        finally
        {
            IsStarting = false;
            StartSessionAndSendCommand.NotifyCanExecuteChanged();
        }
    }

    private string? ResolveDefaultCwd()
    {
        var pending = _nav.ConsumePendingProjectRootPath();
        string? lastSelectedRoot = null;

        var projectId = _preferences.LastSelectedProjectId;
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var project = _preferences.Projects.FirstOrDefault(p => string.Equals(p.ProjectId, projectId, StringComparison.Ordinal));
            if (project != null && !string.IsNullOrWhiteSpace(project.RootPath))
            {
                lastSelectedRoot = project.RootPath;
            }
        }

        // Fallback: if no project selected, keep it unclassified.
        return SessionCwdResolver.Resolve(pending, lastSelectedRoot);
    }
}
