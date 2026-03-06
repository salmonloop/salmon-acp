using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnoAcpClient.Application.Services.Chat;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Domain.Models.Content;
using UnoAcpClient.Domain.Models.Protocol;
using UnoAcpClient.Domain.Models.Session;
using UnoAcpClient.Domain.Services;

namespace UnoAcpClient.Presentation.ViewModels.Chat
{
    /// <summary>
    /// Chat ViewModel，管理会话、消息显示、权限请求等 UI 逻辑
    /// 这是重构后的主要 ViewModel，使用新的 ACP 协议 API
    /// </summary>
    public partial class ChatViewModel : ViewModelBase, IDisposable
    {
        private readonly IChatService _chatService;
        private readonly SynchronizationContext _syncContext;
        private bool _disposed;

        [ObservableProperty]
        private ObservableCollection<ChatMessageViewModel> _messageHistory = new();

        [ObservableProperty]
        private string _currentPrompt = string.Empty;

        [ObservableProperty]
        private string? _currentSessionId;

        [ObservableProperty]
        private bool _isSessionActive;

        [ObservableProperty]
        private string? _agentName;

        [ObservableProperty]
        private string? _agentVersion;

        [ObservableProperty]
        private bool _isInitializing;

        [ObservableProperty]
        private bool _isConnecting;

        [ObservableProperty]
        private ObservableCollection<SessionModeViewModel> _availableModes = new();

        [ObservableProperty]
        private SessionModeViewModel? _selectedMode;

        [ObservableProperty]
        private ObservableCollection<PlanEntryViewModel> _currentPlan = new();

        [ObservableProperty]
        private bool _showPlanPanel;

        [ObservableProperty]
        private bool _showPermissionDialog;

        [ObservableProperty]
        private PermissionRequestViewModel? _pendingPermissionRequest;

        [ObservableProperty]
        private bool _showFileSystemDialog;

        [ObservableProperty]
        private FileSystemRequestViewModel? _pendingFileSystemRequest;

        public string? CurrentConnectionStatus { get; private set; }

        public ChatViewModel(
            IChatService chatService,
            ILogger<ChatViewModel> logger) : base(logger)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

            // 订阅事件
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _chatService.SessionUpdateReceived += OnSessionUpdateReceived;
            _chatService.PermissionRequestReceived += OnPermissionRequestReceived;
            _chatService.FileSystemRequestReceived += OnFileSystemRequestReceived;
            _chatService.ErrorOccurred += OnErrorOccurred;

            // 监听初始化状态变化
            if (_chatService.IsInitialized)
            {
                UpdateAgentInfo();
            }

            // 监听会话状态
            if (_chatService.CurrentSessionId != null)
            {
                CurrentSessionId = _chatService.CurrentSessionId;
                IsSessionActive = true;
                LoadSessionHistory();
            }
        }

        private void OnSessionUpdateReceived(object? sender, SessionUpdateEventArgs e)
        {
            _syncContext.Post(_ =>
            {
                try
                {
                    if (e.Update is AgentMessageUpdate messageUpdate && messageUpdate.Content != null)
                    {
                        AddMessageToHistory(messageUpdate.Content, isOutgoing: false);
                    }
                    else if (e.Update is ToolCallUpdate toolCallUpdate)
                    {
                        AddToolCallToHistory(toolCallUpdate);
                    }
                    else if (e.Update is PlanUpdate planUpdate)
                    {
                        UpdatePlan(planUpdate);
                    }
                    else if (e.Update is ModeChangeUpdate modeChange)
                    {
                        OnModeChanged(modeChange);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "处理会话更新时出错");
                }
            }, null);
        }

        private void OnPermissionRequestReceived(object? sender, PermissionRequestEventArgs e)
        {
            _syncContext.Post(_ =>
            {
                try
                {
                    var viewModel = new PermissionRequestViewModel
                    {
                        MessageId = e.MessageId,
                        SessionId = e.SessionId,
                        ToolCallJson = e.ToolCall?.ToString() ?? string.Empty,
                        Options = new ObservableCollection<PermissionOptionViewModel>(
                            e.Options.Select(opt => new PermissionOptionViewModel
                            {
                                OptionId = opt.OptionId,
                                Name = opt.Name,
                                Kind = opt.Kind
                            }))
                    };

                    // 设置响应回调
                    viewModel.OnRespond = async (outcome, optionId) =>
                    {
                        await _chatService.RespondToPermissionRequestAsync(e.MessageId, outcome, optionId);
                        ShowPermissionDialog = false;
                        PendingPermissionRequest = null;
                    };

                    PendingPermissionRequest = viewModel;
                    ShowPermissionDialog = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "处理权限请求时出错");
                }
            }, null);
        }

        private void OnFileSystemRequestReceived(object? sender, FileSystemRequestEventArgs e)
        {
            _syncContext.Post(_ =>
            {
                try
                {
                    var viewModel = new FileSystemRequestViewModel
                    {
                        MessageId = e.MessageId,
                        SessionId = e.SessionId,
                        Operation = e.Operation,
                        Path = e.Path,
                        Encoding = e.Encoding,
                        Content = e.Content
                    };

                    // 设置响应回调
                    viewModel.OnRespond = async (success, content, message) =>
                    {
                        await _chatService.RespondToFileSystemRequestAsync(e.MessageId, success, content, message);
                        ShowFileSystemDialog = false;
                        PendingFileSystemRequest = null;
                    };

                    PendingFileSystemRequest = viewModel;
                    ShowFileSystemDialog = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "处理文件系统请求时出错");
                }
            }, null);
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            _syncContext.Post(_ =>
            {
                SetError(error);
                Logger.LogError(error);
            }, null);
        }

        private void UpdateAgentInfo()
        {
            if (_chatService.AgentInfo != null)
            {
                AgentName = _chatService.AgentInfo.Name;
                AgentVersion = _chatService.AgentInfo.Version;
            }
        }

        private void LoadSessionHistory()
        {
            MessageHistory.Clear();
            foreach (var entry in _chatService.SessionHistory)
            {
                AddEntryToMessageHistory(entry);
            }
        }

        private void AddEntryToMessageHistory(SessionUpdateEntry entry)
        {
            if (entry.Content != null)
            {
                AddMessageToHistory(entry.Content, isOutgoing: false);
            }
            else if (entry.Entries != null)
            {
                foreach (var planEntry in entry.Entries)
                {
                    var message = ChatMessageViewModel.CreateFromPlanEntry(
                        Guid.NewGuid().ToString(),
                        planEntry,
                        isOutgoing: false);
                    MessageHistory.Add(message);
                }
            }
            else if (!string.IsNullOrEmpty(entry.ModeId))
            {
                var message = ChatMessageViewModel.CreateFromModeChange(
                    Guid.NewGuid().ToString(),
                    entry.ModeId,
                    entry.Title,
                    isOutgoing: false);
                MessageHistory.Add(message);
            }
        }

        private void AddMessageToHistory(ContentBlock content, bool isOutgoing)
        {
            var id = Guid.NewGuid().ToString();
            ChatMessageViewModel message;

            switch (content)
            {
                case TextContentBlock text:
                    message = ChatMessageViewModel.CreateFromTextContent(id, content, isOutgoing);
                    break;
                case ImageContentBlock image:
                    message = ChatMessageViewModel.CreateFromImageContent(id, content, isOutgoing);
                    break;
                case AudioContentBlock audio:
                    message = ChatMessageViewModel.CreateFromAudioContent(id, content, isOutgoing);
                    break;
                default:
                    message = ChatMessageViewModel.CreateFromTextContent(id, content, isOutgoing);
                    break;
            }

            MessageHistory.Add(message);
        }

        private void AddToolCallToHistory(ToolCallUpdate toolCall)
        {
            var message = ChatMessageViewModel.CreateFromToolCall(
                Guid.NewGuid().ToString(),
                toolCall.ToolCallId,
                toolCall.ToolCall?.GetRawText(),
                toolCall.Kind,
                toolCall.Status,
                toolCall.Title,
                isOutgoing: false);
            MessageHistory.Add(message);
        }

        private void UpdatePlan(PlanUpdate planUpdate)
        {
            ShowPlanPanel = true;
            CurrentPlan.Clear();

            if (planUpdate.Entries != null)
            {
                foreach (var entry in planUpdate.Entries)
                {
                    CurrentPlan.Add(new PlanEntryViewModel
                    {
                        Content = entry.Content ?? string.Empty,
                        Status = entry.Status,
                        Priority = entry.Priority
                    });
                }
            }
        }

        private void OnModeChanged(ModeChangeUpdate modeChange)
        {
            if (!string.IsNullOrEmpty(modeChange.ModeId))
            {
                // 更新当前模式
                var selectedMode = AvailableModes.FirstOrDefault(m => m.ModeId == modeChange.ModeId);
                if (selectedMode != null)
                {
                    SelectedMode = selectedMode;
                }
            }
        }

        [RelayCommand]
        private async Task InitializeAndConnectAsync()
        {
            if (IsInitializing || IsConnecting)
                return;

            try
            {
                IsInitializing = true;
                ClearError();

                // 初始化 ACP 客户端
                var initParams = new InitializeParams
                {
                    ProtocolVersion = 1,
                    ClientInfo = new ClientInfo
                    {
                        Name = "UnoAcpClient",
                        Title = "Uno ACP Client",
                        Version = "1.0.0"
                    },
                    ClientCapabilities = new ClientCapabilities
                    {
                        Fs = new FsCapability
                        {
                            ReadTextFile = true,
                            WriteTextFile = true
                        }
                    }
                };

                var response = await _chatService.InitializeAsync(initParams);
                UpdateAgentInfo();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化失败");
                SetError($"初始化失败：{ex.Message}");
            }
            finally
            {
                IsInitializing = false;
            }
        }

        [RelayCommand]
        private async Task CreateNewSessionAsync()
        {
            if (IsConnecting)
                return;

            try
            {
                IsConnecting = true;
                ClearError();

                var sessionParams = new SessionNewParams
                {
                    Cwd = Environment.CurrentDirectory,
                    McpServers = null // 可以根据配置添加 MCP 服务器
                };

                var response = await _chatService.CreateSessionAsync(sessionParams);
                CurrentSessionId = response.SessionId;
                IsSessionActive = true;

                // 加载可用模式
                if (response.Modes != null)
                {
                    AvailableModes.Clear();
                    foreach (var mode in response.Modes)
                    {
                        AvailableModes.Add(new SessionModeViewModel
                        {
                            ModeId = mode.Id,
                            ModeName = mode.Name,
                            Description = mode.Description
                        });
                    }

                    // 选择第一个模式作为默认
                    if (AvailableModes.Count > 0)
                    {
                        SelectedMode = AvailableModes[0];
                    }
                }

                MessageHistory.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建会话失败");
                SetError($"创建会话失败：{ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSendPrompt))]
        private async Task SendPromptAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentPrompt) || !IsSessionActive)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                // 添加用户消息到历史
                var userContent = new TextContentBlock { Text = CurrentPrompt };
                AddMessageToHistory(userContent, isOutgoing: true);

                var promptParams = new SessionPromptParams
                {
                    SessionId = CurrentSessionId!,
                    Prompt = CurrentPrompt,
                    MaxTokens = null,
                    StopSequences = null
                };

                await _chatService.SendPromptAsync(promptParams);
                CurrentPrompt = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发送提示失败");
                SetError($"发送提示失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSendPrompt() => IsSessionActive && !string.IsNullOrWhiteSpace(CurrentPrompt) && !IsBusy;

        [RelayCommand]
        private async Task SetModeAsync(SessionModeViewModel? mode)
        {
            if (mode == null || !IsSessionActive)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                var modeParams = new SessionSetModeParams
                {
                    SessionId = CurrentSessionId!,
                    ModeId = mode.ModeId
                };

                await _chatService.SetSessionModeAsync(modeParams);
                SelectedMode = mode;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换模式失败");
                SetError($"切换模式失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelSessionAsync()
        {
            if (!IsSessionActive)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                var cancelParams = new SessionCancelParams
                {
                    SessionId = CurrentSessionId!,
                    Reason = "User cancelled"
                };

                await _chatService.CancelSessionAsync(cancelParams);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消会话失败");
                SetError($"取消会话失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ClearHistory()
        {
            MessageHistory.Clear();
            CurrentPlan.Clear();
            ShowPlanPanel = false;
            _chatService.ClearHistory();
        }

        [RelayCommand]
        private async Task DisconnectAsync()
        {
            try
            {
                IsBusy = true;
                ClearError();

                await _chatService.DisconnectAsync();
                CurrentSessionId = null;
                IsSessionActive = false;
                MessageHistory.Clear();
                CurrentPlan.Clear();
                AvailableModes.Clear();
                SelectedMode = null;
                AgentName = null;
                AgentVersion = null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "断开连接失败");
                SetError($"断开连接失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnCurrentPromptChanged(string value)
        {
            SendPromptCommand.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _chatService.SessionUpdateReceived -= OnSessionUpdateReceived;
                    _chatService.PermissionRequestReceived -= OnPermissionRequestReceived;
                    _chatService.FileSystemRequestReceived -= OnFileSystemRequestReceived;
                    _chatService.ErrorOccurred -= OnErrorOccurred;
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 会话模式 ViewModel
    /// </summary>
    public partial class SessionModeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _modeId = string.Empty;

        [ObservableProperty]
        private string _modeName = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;
    }

    /// <summary>
    /// 权限请求 ViewModel
    /// </summary>
    public partial class PermissionRequestViewModel : ObservableObject
    {
        public object MessageId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string ToolCallJson { get; set; } = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PermissionOptionViewModel> _options = new();

        public Func<string, string?, Task>? OnRespond { get; set; }

        [RelayCommand]
        private async Task RespondAsync(PermissionOptionViewModel? option)
        {
            if (OnRespond == null)
                return;

            if (option != null)
            {
                await OnRespond("selected", option.OptionId);
            }
            else
            {
                await OnRespond("cancelled", null);
            }
        }
    }

    /// <summary>
    /// 权限选项 ViewModel
    /// </summary>
    public partial class PermissionOptionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _optionId = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _kind = string.Empty;
    }

    /// <summary>
    /// 文件系统请求 ViewModel
    /// </summary>
    public partial class FileSystemRequestViewModel : ObservableObject
    {
        public object MessageId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Encoding { get; set; }
        public string? Content { get; set; }

        public Func<bool, string?, string?, Task>? OnRespond { get; set; }

        [ObservableProperty]
        private string _responseContent = string.Empty;

        [RelayCommand]
        private async Task RespondAsync(bool success)
        {
            if (OnRespond == null)
                return;

            await OnRespond(success, success ? ResponseContent : null, success ? null : "Operation failed");
        }
    }
}
