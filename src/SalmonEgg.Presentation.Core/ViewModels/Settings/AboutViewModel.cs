using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Resources;
using SalmonEgg.Presentation.Services;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed partial class AboutViewModel : ObservableObject
{
    private readonly IPlatformShellService _shell;
    private readonly IStorageLocationService _storageLocations;
    private readonly IAppDataService _paths;
    private readonly IAppDocumentService _documents;
    private readonly IUiInteractionService _ui;
    private readonly IStringLocalizer<CoreStrings> _localizer;

    public string AppName => "SalmonEgg";

    public string AppVersion => System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "unknown";

    public string ProtocolVersion => new InitializeParams().ProtocolVersion.ToString();

    public string DocsRootPath => _documents.DocsRootPath;

    public AboutViewModel(
        IPlatformShellService shell,
        IStorageLocationService storageLocations,
        IAppDataService paths,
        IAppDocumentService documents,
        IUiInteractionService ui,
        IStringLocalizer<CoreStrings> localizer)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _storageLocations = storageLocations ?? throw new ArgumentNullException(nameof(storageLocations));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    [RelayCommand]
    private Task OpenAppDataFolderAsync()
    {
        return _storageLocations.OpenAsync(AppStorageLocation.AppData);
    }

    [RelayCommand]
    private async Task OpenReleaseNotesAsync()
    {
        var path = _documents.GetReleaseNotesPath();
        if (!await _documents.ExistsAsync(path))
        {
            await NotifyMissingDocAsync(path, _localizer["About_ReleaseNotesTitle"]);
            return;
        }

        await _shell.OpenFileAsync(path);
    }

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync()
    {
        var path = _documents.GetPrivacyPolicyPath();
        if (!await _documents.ExistsAsync(path))
        {
            await NotifyMissingDocAsync(path, _localizer["About_PrivacyPolicyTitle"]);
            return;
        }

        await _shell.OpenFileAsync(path);
    }

    [RelayCommand]
    private async Task CopyVersionInfoAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{_localizer["About_VersionInfoAppLabel"]}: {AppName}");
        sb.AppendLine($"{_localizer["About_VersionInfoVersionLabel"]}: {AppVersion}");
        sb.AppendLine($"{_localizer["About_VersionInfoProtocolLabel"]}: {ProtocolVersion}");
        var copied = await _shell.CopyToClipboardAsync(sb.ToString());
        await _ui.ShowInfoAsync(copied
                ? _localizer["About_VersionInfoCopied"]
                : _localizer["About_ClipboardUnsupported"]);
    }

    private async Task NotifyMissingDocAsync(string path, string title)
    {
        var folder = System.IO.Path.GetDirectoryName(path);
        var message = folder == null
            ? _localizer["About_MissingDocumentMessage", title]
            : _localizer["About_MissingDocumentWithFolderMessage", title, folder];

        await _ui.ShowInfoAsync(message);

        if (!string.IsNullOrWhiteSpace(folder))
        {
            await _storageLocations.OpenExistingFolderAsync(folder);
        }
    }
}
