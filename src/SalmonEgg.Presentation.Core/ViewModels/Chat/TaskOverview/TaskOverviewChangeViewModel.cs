using CommunityToolkit.Mvvm.ComponentModel;
namespace SalmonEgg.Presentation.Core.ViewModels.Chat.TaskOverview;

public sealed partial class TaskOverviewChangeViewModel : ObservableObject
{
    [ObservableProperty]
    private TaskOverviewChangeKind _kind = TaskOverviewChangeKind.Changed;

    [ObservableProperty]
    private string _lineSummary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DirectoryPath))]
    [NotifyPropertyChangedFor(nameof(FileName))]
    private string _path = string.Empty;

    public string DirectoryPath => TaskOverviewPathPresenter.Present(Path).DirectoryPath;

    public string FileName => TaskOverviewPathPresenter.Present(Path).FileName;
}
