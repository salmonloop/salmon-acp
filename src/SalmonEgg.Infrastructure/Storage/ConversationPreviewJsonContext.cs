using System.Text.Json.Serialization;
using SalmonEgg.Domain.Models.ConversationPreview;

namespace SalmonEgg.Infrastructure.Storage;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
[JsonSerializable(typeof(ConversationPreviewSnapshot))]
public partial class ConversationPreviewJsonContext : JsonSerializerContext
{
}
