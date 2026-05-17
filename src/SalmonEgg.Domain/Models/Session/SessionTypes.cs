using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalmonEgg.Domain.Models.Session
{
    /// <summary>
    /// 会话状态的枚举。
    /// 表示会话在其生命周期中的当前状态。
    /// </summary>
    [JsonConverter(typeof(SessionStateJsonConverter))]
    public enum SessionState
    {
        /// <summary>
        /// 会话处于活动状态，正在处理请求。
        /// </summary>
        Active,

        /// <summary>
        /// 会话正在等待用户输入或外部事件。
        /// </summary>
        Waiting,

        /// <summary>
        /// 会话已被用户取消。
        /// </summary>
        Cancelled,

        /// <summary>
        /// 会话已成功完成。
        /// </summary>
        Completed,

        /// <summary>
        /// 会话因错误而终止。
        /// </summary>
        Error
    }

    /// <summary>
    /// 停止原因的枚举。
    /// 表示 Agent 生成响应时停止的原因。
    /// </summary>
    [JsonConverter(typeof(StopReasonJsonConverter))]
    public enum StopReason
    {
        /// <summary>
        /// 正常结束回合，Agent 完成了回复。
        /// </summary>
        EndTurn,

        /// <summary>
        /// 达到最大令牌数限制。
        /// </summary>
        MaxTokens,

        /// <summary>
        /// 在一次回合中超出了最大请求次数。
        /// </summary>
        MaxTurnRequests,

        /// <summary>
        /// Agent 拒绝回答问题。
        /// </summary>
        Refusal,

        /// <summary>
        /// 用户或客户端取消了回合。
        /// </summary>
        Cancelled
    }

    public sealed class SessionStateJsonConverter : JsonConverter<SessionState>
    {
        public override SessionState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Session state must be a string.");
            }

            return reader.GetString() switch
            {
                "active" => SessionState.Active,
                "waiting" => SessionState.Waiting,
                "cancelled" => SessionState.Cancelled,
                "completed" => SessionState.Completed,
                "error" => SessionState.Error,
                var value => throw new JsonException($"Unsupported session state '{value}'.")
            };
        }

        public override void Write(Utf8JsonWriter writer, SessionState value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                SessionState.Active => "active",
                SessionState.Waiting => "waiting",
                SessionState.Cancelled => "cancelled",
                SessionState.Completed => "completed",
                SessionState.Error => "error",
                _ => throw new JsonException($"Unsupported session state '{value}'.")
            });
        }
    }

    public sealed class StopReasonJsonConverter : JsonConverter<StopReason>
    {
        public override StopReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Stop reason must be a string.");
            }

            return reader.GetString() switch
            {
                "end_turn" => StopReason.EndTurn,
                "max_tokens" => StopReason.MaxTokens,
                "max_turn_requests" => StopReason.MaxTurnRequests,
                "refusal" => StopReason.Refusal,
                "cancelled" => StopReason.Cancelled,
                var value => throw new JsonException($"Unsupported stop reason '{value}'.")
            };
        }

        public override void Write(Utf8JsonWriter writer, StopReason value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                StopReason.EndTurn => "end_turn",
                StopReason.MaxTokens => "max_tokens",
                StopReason.MaxTurnRequests => "max_turn_requests",
                StopReason.Refusal => "refusal",
                StopReason.Cancelled => "cancelled",
                _ => throw new JsonException($"Unsupported stop reason '{value}'.")
            });
        }
    }
}
