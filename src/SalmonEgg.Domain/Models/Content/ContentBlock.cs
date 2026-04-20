using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalmonEgg.Domain.Models.Content
{
    /// <summary>
    /// 内容块的基类。
    /// 用于表示会话中的各种类型的内容（文本、图片、音频、资源等）。
    /// 使用 JsonPolymorphic 特性支持多态序列化。
    /// 注意：Type 属性由派生类覆盖；基类保留未知内容的扩展数据。
    /// 序列化时的 "type" 字段由 [JsonPolymorphic] 自动处理，无需在属性上添加 [JsonPropertyName]。
    /// </summary>
    [JsonPolymorphic(
        TypeDiscriminatorPropertyName = "type",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType,
        IgnoreUnrecognizedTypeDiscriminators = true)]
    [JsonDerivedType(typeof(TextContentBlock), "text")]
    [JsonDerivedType(typeof(ImageContentBlock), "image")]
    [JsonDerivedType(typeof(AudioContentBlock), "audio")]
    [JsonDerivedType(typeof(ResourceLinkContentBlock), "resource_link")]
    [JsonDerivedType(typeof(ResourceContentBlock), "resource")]
    public class ContentBlock
    {
        /// <summary>
        /// Optional ACP annotations that guide how the content should be used or displayed.
        /// </summary>
        [JsonPropertyName("annotations")]
        public Annotations? Annotations { get; set; }

        /// <summary>
        /// Preserves unknown payload members when the content discriminator is not recognized.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        /// <summary>
        /// 内容块的类型标识符。
        /// 用于多态序列化和反序列化。
        /// 由 [JsonPolymorphic] 自动序列化为 JSON 中的 "type" 字段。
        /// 此属性被 [JsonIgnore] 忽略，因为类型信息已由 JsonPolymorphic 自动处理。
        /// </summary>
        [JsonIgnore]
        public virtual string Type => string.Empty;
    }

    /// <summary>
    /// Optional ACP annotations attached to a content block.
    /// </summary>
    public sealed class Annotations
    {
        /// <summary>
        /// Intended audience for the content.
        /// </summary>
        [JsonPropertyName("audience")]
        public List<string>? Audience { get; set; }

        /// <summary>
        /// Relative priority from 0.0 to 1.0.
        /// </summary>
        [JsonPropertyName("priority")]
        public decimal? Priority { get; set; }

        /// <summary>
        /// ISO 8601 timestamp for the last modification time.
        /// </summary>
        [JsonPropertyName("lastModified")]
        public string? LastModified { get; set; }
    }
}
