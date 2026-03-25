using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Services;

namespace SalmonEgg.Presentation.ViewModels.Discover;

public sealed partial class DiscoverSessionsViewModel : ObservableObject
{
    private readonly ILogger<DiscoverSessionsViewModel> _logger;
    private readonly INavigationCoordinator _navigationCoordinator;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private AgentViewModel? _selectedAgent;

    public ObservableCollection<AgentViewModel> AvailableAgents { get; } = new();

    public ObservableCollection<DiscoverSessionItemViewModel> AgentSessions { get; } = new();

    public DiscoverSessionsViewModel(
        ILogger<DiscoverSessionsViewModel> logger,
        INavigationCoordinator navigationCoordinator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
    }

    [RelayCommand]
    private async Task LoadAgentsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // TODO: In a real implementation, we would call an IAgentService to get available agents.
            // For now, we will add a mock agent for demonstration purposes.
            AvailableAgents.Clear();
            AvailableAgents.Add(new AgentViewModel("mock-agent-1", "Local Assistant Agent", "A helpful local agent"));

            if (AvailableAgents.Any())
            {
                SelectedAgent = AvailableAgents.First();
                await LoadSessionsForAgentAsync(SelectedAgent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agents");
            ErrorMessage = "无法加载Agent列表，请重试。";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedAgentChanged(AgentViewModel? value)
    {
        if (value != null)
        {
            _ = LoadSessionsForAgentAsync(value);
        }
        else
        {
            AgentSessions.Clear();
        }
    }

    private async Task LoadSessionsForAgentAsync(AgentViewModel agent)
    {
        IsLoading = true;
        ErrorMessage = null;
        AgentSessions.Clear();

        try
        {
            // TODO: In a real implementation, we would load the sessions from the specific agent.
            // For now, mock data.
            await Task.Delay(500); // Simulate network/disk latency

            AgentSessions.Add(new DiscoverSessionItemViewModel(
                "session-1",
                "代码重构计划",
                "讨论关于如何重构MainNavigationViewModel的计划",
                DateTime.Now.AddHours(-2)));

            AgentSessions.Add(new DiscoverSessionItemViewModel(
                "session-2",
                "BUG修复: 导航闪退",
                "修复了在快速切换项目时可能发生的闪退问题",
                DateTime.Now.AddDays(-1)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions for agent {AgentId}", agent.Id);
            ErrorMessage = "无法加载该Agent的会话列表。";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadSessionAsync(DiscoverSessionItemViewModel? session)
    {
        if (session == null) return;

        try
        {
            IsLoading = true;

            // TODO: In a real implementation we would import the session from the agent
            // into our own chat session catalog, then activate it.
            // For now, we just log it.
            _logger.LogInformation("Loading session {SessionId} from agent {AgentId}",
                session.Id, SelectedAgent?.Id);

            // After import, we'd navigate to it:
            // await _navigationCoordinator.ActivateSessionAsync(importedSessionId, projectId);

            await Task.Delay(1000); // Simulating import
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import and load session {SessionId}", session.Id);
            ErrorMessage = "加载会话失败，请重试。";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public sealed class AgentViewModel
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    public AgentViewModel(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}

public sealed class DiscoverSessionItemViewModel
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime LastModified { get; }
    public string FormattedDate => LastModified.ToString("yyyy-MM-dd HH:mm");

    public DiscoverSessionItemViewModel(string id, string title, string description, DateTime lastModified)
    {
        Id = id;
        Title = title;
        Description = description;
        LastModified = lastModified;
    }
}