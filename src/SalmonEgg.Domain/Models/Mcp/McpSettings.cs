using System.Collections.Generic;

namespace SalmonEgg.Domain.Models.Mcp
{
    /// <summary>
    /// 全局 MCP 设置。
    /// </summary>
    public sealed class McpSettings
    {
        /// <summary>
        /// 是否在 ACP 会话请求中包含全局 MCP servers。
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 全局 MCP server catalog。
        /// </summary>
        public List<McpServer> Servers { get; set; } = new List<McpServer>();
    }
}
