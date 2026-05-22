namespace SalmonEgg.Presentation.Core.ViewModels.Chat.TaskOverview;

public static class TaskOverviewPathPresenter
{
    public static TaskOverviewPathParts Present(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new TaskOverviewPathParts(string.Empty, string.Empty);
        }

        var normalized = path.Trim().TrimEnd('/', '\\');
        if (normalized.Length == 0)
        {
            return new TaskOverviewPathParts(string.Empty, string.Empty);
        }

        var lastSeparatorIndex = normalized.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSeparatorIndex < 0)
        {
            return new TaskOverviewPathParts(normalized, string.Empty);
        }

        var fileName = normalized[(lastSeparatorIndex + 1)..];
        var directory = normalized[..lastSeparatorIndex];

        return new TaskOverviewPathParts(fileName, directory);
    }
}

public sealed record TaskOverviewPathParts(string FileName, string DirectoryPath);
