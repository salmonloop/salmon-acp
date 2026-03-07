using CommunityToolkit.Mvvm.ComponentModel;
using UnoAcpClient.Domain.Models.Content;

namespace UnoAcpClient.Presentation.ViewModels.Chat;

/// <summary>
/// 资源内容 ViewModel，用于在 UI 中展示资源内容块和资源链接。
/// 封装了 ResourceContentBlock 和 ResourceLinkContentBlock 的显示逻辑。
/// </summary>
public partial class ResourceViewModel : ObservableObject
{
    /// <summary>
    /// 资源的 URI
    /// </summary>
    [ObservableProperty]
    private string _uri = string.Empty;

    /// <summary>
    /// 资源名称（如果未提供，则使用 URI 显示）
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// MIME 类型
    /// </summary>
    [ObservableProperty]
    private string _mimeType = string.Empty;

    /// <summary>
    /// 嵌入的资源内容（仅适用于 ResourceContentBlock）
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// 资源链接的显示文本（仅适用于 ResourceLinkContentBlock）
    /// </summary>
    [ObservableProperty]
    private string _linkText = string.Empty;

    /// <summary>
    /// 资源标题
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// 资源描述
    /// </summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// 获取资源的显示标题
    /// </summary>
    public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : (!string.IsNullOrEmpty(Name) ? Name : Uri);

    /// <summary>
    /// 判断是否为嵌入的资源内容
    /// </summary>
    public bool IsResourceContent => !string.IsNullOrEmpty(Content);

    /// <summary>
    /// 判断是否为资源链接
    /// </summary>
    public bool IsResourceLink => !string.IsNullOrEmpty(LinkText);

    /// <summary>
    /// 获取显示用的文本内容
    /// </summary>
    public string GetDisplayContent() => IsResourceContent ? Content : LinkText;

   /// <summary>
   /// 从 ResourceContentBlock 创建 ResourceViewModel
   /// </summary>
   public static ResourceViewModel CreateFromContent(ResourceContentBlock block)
   {
       return new ResourceViewModel
       {
           Uri = block.Resource.Uri,
           Name = "Unknown Resource",
           MimeType = block.Resource.MimeType ?? "text/plain",
           Content = block.Resource.Text ?? block.Resource.Blob ?? string.Empty,
           Title = "Resource Content",
           Description = null
       };
   }

   /// <summary>
   /// 从 ResourceLinkContentBlock 创建 ResourceViewModel
   /// </summary>
   public static ResourceViewModel CreateFromLink(ResourceLinkContentBlock block)
   {
       return new ResourceViewModel
       {
           Uri = block.Uri,
           Name = block.Name ?? "Resource Link",
           MimeType = block.MimeType ?? string.Empty,
           LinkText = block.Name ?? block.Uri,
           Title = block.Title ?? "Link",
           Description = block.Description
       };
   }
}
