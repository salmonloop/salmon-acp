using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using UnoAcpClient.Application.Common;
using UnoAcpClient.Application.UseCases;
using UnoAcpClient.Domain.Models;
using UnoAcpClient.Domain.Services;

namespace UnoAcpClient.Application.Services
{
    /// <summary>
    /// 连接服务实现
    /// 封装连接和断开用例，提供当前连接状态查询
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly ConnectToServerUseCase _connectUseCase;
        private readonly DisconnectUseCase _disconnectUseCase;
        private readonly IConnectionManager _connectionManager;
        private ConnectionState _currentState;

        /// <summary>
        /// 初始化 ConnectionService 的新实例
        /// </summary>
        /// <param name="connectUseCase">连接到服务器用例</param>
        /// <param name="disconnectUseCase">断开连接用例</param>
        /// <param name="connectionManager">连接管理器</param>
        public ConnectionService(
            ConnectToServerUseCase connectUseCase,
            DisconnectUseCase disconnectUseCase,
            IConnectionManager connectionManager)
        {
            _connectUseCase = connectUseCase ?? throw new ArgumentNullException(nameof(connectUseCase));
            _disconnectUseCase = disconnectUseCase ?? throw new ArgumentNullException(nameof(disconnectUseCase));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));

            // 初始化当前状态为断开连接
            _currentState = new ConnectionState
            {
                Status = ConnectionStatus.Disconnected
            };

            // 订阅连接状态变化以保持当前状态同步 (Requirement 3.5)
            _connectionManager.ConnectionStateChanges
                .Subscribe(state =>
                {
                    _currentState = state;
                });
        }

        /// <summary>
        /// 异步连接到服务器
        /// </summary>
        /// <param name="configId">服务器配置 ID</param>
        /// <returns>操作结果</returns>
        public async Task<Result> ConnectAsync(string configId)
        {
            // 封装 ConnectToServerUseCase (Requirement 3.1)
            return await _connectUseCase.ExecuteAsync(configId);
        }

        /// <summary>
        /// 异步断开与服务器的连接
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        public async Task DisconnectAsync()
        {
            // 封装 DisconnectUseCase (Requirement 3.1)
            await _disconnectUseCase.ExecuteAsync();
        }

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        /// <returns>当前连接状态</returns>
        public ConnectionState GetCurrentState()
        {
            // 提供当前连接状态查询 (Requirement 3.5)
            return _currentState;
        }
    }
}
