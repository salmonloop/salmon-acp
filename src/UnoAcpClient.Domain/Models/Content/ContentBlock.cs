using System.Text.Json.Serialization;

namespace UnoAcpClient.Domain.Models.Content
{
    /// <summary>
    /// 内容块的抽象基类。
    /// 用于表示会话中的各种类型的内容（文本、图片、音频、资源等）。
    /// 使用 JsonPolymorphic 特性支持多态序列化。
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TextContentBlock), "text")]
    [JsonDerivedType(typeof(ImageContentBlock), "image")]
    [JsonDerivedType(typeof(AudioContentBlock), "audio")]
    [JsonDerivedType(typeof(ResourceLinkContentBlock), "resourceLink")]
    [JsonDerivedType(typeof(ResourceContentBlock), "resource")]
    public abstract class ContentBlock
    {
        /// <summary>
        /// 内容块的类型标识符。
        /// 用于多态序列化和反序列化。
        /// </summary>
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }
}
