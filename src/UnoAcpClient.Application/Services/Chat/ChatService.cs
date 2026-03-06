using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnoAcpClient.Domain.Models.Content;
using UnoAcpClient.Domain.Models.Plan;
using UnoAcpClient.Domain.Models.Protocol;
using UnoAcpClient.Domain.Models.Session;
using UnoAcpClient.Domain.Models.Tool;
using UnoAcpClient.Domain.Services;
using UnoAcpClient.Domain.Services.Security;

namespace UnoAcpClient.Application.Services.Chat
{
    /// <summary>
    /// Chat 服务实现类
    /// 封装了 ACP 客户端的核心功能，提供聊天相关的服务
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IAcpClient _acpClient;
        private readonly IErrorLogger _errorLogger;

        private string? _currentSessionId;
        private Plan? _currentPlan;
        private SessionModeState? _currentMode;
        private readonly ObservableCollection<SessionUpdateEntry> _sessionHistory;

        public string? CurrentSessionId => _currentSessionId;
        public bool IsInitialized => _acpClient.IsInitialized;
        public bool IsConnected => _acpClient.IsConnected;
        public AgentInfo? AgentInfo => _acpClient.AgentInfo;
        public AgentCapabilities? AgentCapabilities => _acpClient.AgentCapabilities;
        public IReadOnlyList<SessionUpdateEntry> SessionHistory => _sessionHistory;
        public Plan? CurrentPlan => _currentPlan;
        public SessionModeState? CurrentMode => _currentMode;

        public event EventHandler<SessionUpdateEventArgs>? SessionUpdateReceived;
        public event EventHandler<PermissionRequestEventArgs>? PermissionRequestReceived;
        public event EventHandler<FileSystemRequestEventArgs>? FileSystemRequestReceived;
        public event EventHandler<string>? ErrorOccurred;

        public ChatService(IAcpClient acpClient, IErrorLogger? errorLogger = null)
        {
            _acpClient = acpClient ?? throw new ArgumentNullException(nameof(acpClient));
            _errorLogger = errorLogger ?? new Domain.Services.ErrorLogger();
            _sessionHistory = new ObservableCollection<SessionUpdateEntry>();

            // 订阅 ACP 客户端事件
            _acpClient.SessionUpdateReceived += OnSessionUpdateReceived;
            _acpClient.PermissionRequestReceived += OnPermissionRequestReceived;
            _acpClient.FileSystemRequestReceived += OnFileSystemRequestReceived;
            _acpClient.ErrorOccurred += OnErrorOccurred;
        }

        private void OnSessionUpdateReceived(object? sender, SessionUpdateEventArgs e)
        {
            if (e.Update != null)
            {
                // 更新会话历史
                var entry = CreateSessionUpdateEntry(e.Update, e.SessionId);
                if (entry != null)
                {
                    _sessionHistory.Add(entry);

                    // 处理不同类型的更新
                    switch (e.Update)
                    {
                        case AgentMessageUpdate messageUpdate:
                            // 处理消息更新
                            break;
                        case ToolCallUpdate toolCallUpdate:
                            // 处理工具调用更新
                            break;
                        case PlanUpdate planUpdate:
                            // 更新当前计划
                            if (planUpdate.Entries != null)
                            {
                                _currentPlan = new Plan { Entries = planUpdate.Entries };
                            }
                            break;
                        case ModeChangeUpdate modeChange:
                            // 更新当前模式
                            if (!string.IsNullOrEmpty(modeChange.ModeId))
                            {
                                _currentMode = new SessionModeState { CurrentModeId = modeChange.ModeId };
                            }
                            break;
                    }
                }
            }

            SessionUpdateReceived?.Invoke(this, e);
        }

        private void OnPermissionRequestReceived(object? sender, PermissionRequestEventArgs e)
        {
            PermissionRequestReceived?.Invoke(this, e);
        }

        private void OnFileSystemRequestReceived(object? sender, FileSystemRequestEventArgs e)
        {
            FileSystemRequestReceived?.Invoke(this, e);
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            ErrorOccurred?.Invoke(this, error);
            _errorLogger.LogError("ChatService", "Error occurred", error, null, null);
        }

        private SessionUpdateEntry? CreateSessionUpdateEntry(SessionUpdate update, string sessionId)
        {
            var entry = new SessionUpdateEntry
            {
                Timestamp = DateTime.UtcNow,
                SessionUpdateType = update.SessionUpdateType
            };

            switch (update)
            {
                case AgentMessageUpdate messageUpdate:
                    entry.Content = messageUpdate.Content;
                    break;
                case ToolCallUpdate toolCallUpdate:
                    entry.ToolCallId = toolCallUpdate.ToolCallId;
                    entry.ToolCall = toolCallUpdate.ToolCall;
                    entry.Kind = toolCallUpdate.Kind;
                    entry.Status = toolCallUpdate.Status;
                    entry.Title = toolCallUpdate.Title;
                    break;
                case PlanUpdate planUpdate:
                    entry.Entries = planUpdate.Entries;
                    entry.Title = planUpdate.Title;
                    break;
                case ModeChangeUpdate modeChange:
                    entry.ModeId = modeChange.ModeId;
                    entry.Title = modeChange.Title;
                    break;
                case ConfigUpdateUpdate configUpdate:
                    entry.ConfigOptions = configUpdate.ConfigOptions;
                    break;
            }

            return entry;
        }

        public async Task<InitializeResponse> InitializeAsync(InitializeParams @params)
        {
            try
            {
                var response = await _acpClient.InitializeAsync(@params);
                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "InitializeAsync", ex.Message, ex.StackTrace, null);
                throw;
            }
        }

        public async Task<SessionNewResponse> CreateSessionAsync(SessionNewParams @params)
        {
            try
            {
                var response = await _acpClient.CreateSessionAsync(@params);
                _currentSessionId = response.SessionId;
                _sessionHistory.Clear();

                // 保存会话模式信息
                if (response.Modes != null && response.Modes.Count > 0)
                {
                    // 可以选择默认模式
                }

                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "CreateSessionAsync", ex.Message, ex.StackTrace, _currentSessionId);
                throw;
            }
        }

        public async Task<SessionLoadResponse> LoadSessionAsync(SessionLoadParams @params)
        {
            try
            {
                var response = await _acpClient.LoadSessionAsync(@params);
                _currentSessionId = @params.SessionId;
                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "LoadSessionAsync", ex.Message, ex.StackTrace, @params.SessionId);
                throw;
            }
        }

        public async Task<SessionPromptResponse> SendPromptAsync(SessionPromptParams @params)
        {
            try
            {
                var response = await _acpClient.SendPromptAsync(@params);
                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "SendPromptAsync", ex.Message, ex.StackTrace, @params.SessionId);
                throw;
            }
        }

        public async Task<SessionSetModeResponse> SetSessionModeAsync(SessionSetModeParams @params)
        {
            try
            {
                var response = await _acpClient.SetSessionModeAsync(@params);
                if (!string.IsNullOrEmpty(@params.ModeId))
                {
                    _currentMode = new SessionModeState { CurrentModeId = @params.ModeId };
                }
                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "SetSessionModeAsync", ex.Message, ex.StackTrace, @params.SessionId);
                throw;
            }
        }

        public async Task<SessionSetConfigOptionResponse> SetSessionConfigOptionAsync(SessionSetConfigOptionParams @params)
        {
            try
            {
                var response = await _acpClient.SetSessionConfigOptionAsync(@params);
                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "SetSessionConfigOptionAsync", ex.Message, ex.StackTrace, @params.SessionId);
                throw;
            }
        }

        public async Task<SessionCancelResponse> CancelSessionAsync(SessionCancelParams @params)
        {
            try
            {
                var response = await _acpClient.CancelSessionAsync(@params);

                // 更新会话状态
                var sessionIndex = _sessionHistory.Count - 1;
                while (sessionIndex >= 0 && _sessionHistory[sessionIndex].SessionUpdateType != "session_start")
                {
                    sessionIndex--;
                }

                return response;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "CancelSessionAsync", ex.Message, ex.StackTrace, @params.SessionId);
                throw;
            }
        }

        public async Task<bool> RespondToPermissionRequestAsync(object messageId, string outcome, string? optionId = null)
        {
            try
            {
                return await _acpClient.RespondToPermissionRequestAsync(messageId, outcome, optionId);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "RespondToPermissionRequestAsync", ex.Message, ex.StackTrace, null);
                throw;
            }
        }

        public async Task<bool> RespondToFileSystemRequestAsync(object messageId, bool success, string? content = null, string? message = null)
        {
            try
            {
                return await _acpClient.RespondToFileSystemRequestAsync(messageId, success, content, message);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "RespondToFileSystemRequestAsync", ex.Message, ex.StackTrace, null);
                throw;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _currentSessionId = null;
                _currentPlan = null;
                _currentMode = null;
                _sessionHistory.Clear();

                return await _acpClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "DisconnectAsync", ex.Message, ex.StackTrace, null);
                throw;
            }
        }

        public async Task<List<SessionMode>?> GetAvailableModesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentSessionId))
                {
                    return null;
                }

                // 可以通过会话更新事件获取模式信息
                // 这里暂时返回 null，实际实现需要根据响应获取
                return null;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("ChatService", "GetAvailableModesAsync", ex.Message, ex.StackTrace, _currentSessionId);
                throw;
            }
        }

        public void ClearHistory()
        {
            _sessionHistory.Clear();
            _currentPlan = null;
        }

        public void Dispose()
        {
            _acpClient.SessionUpdateReceived -= OnSessionUpdateReceived;
            _acpClient.PermissionRequestReceived -= OnPermissionRequestReceived;
            _acpClient.FileSystemRequestReceived -= OnFileSystemRequestReceived;
            _acpClient.ErrorOccurred -= OnErrorOccurred;
        }
    }
}
