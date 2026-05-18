using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using SalmonEgg.Presentation.Core.Resources;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Models.Search;
using SalmonEgg.Presentation.Models.Settings;

namespace SalmonEgg.Presentation.Core.Services.Search;

public sealed class DefaultGlobalSearchPipeline : IGlobalSearchPipeline
{
    private readonly IStringLocalizer<CoreStrings> _localizer;

    public DefaultGlobalSearchPipeline(IStringLocalizer<CoreStrings> localizer)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public Task<GlobalSearchSnapshot> SearchAsync(
        string query,
        GlobalSearchSourceSnapshot source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new GlobalSearchSnapshot(ImmutableArray<GlobalSearchGroupSnapshot>.Empty));
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        var groups = new List<GlobalSearchGroupSnapshot>();

        var sessions = SearchSessions(normalizedQuery, source.Sessions);
        cancellationToken.ThrowIfCancellationRequested();
        if (sessions.Items.Length > 0)
        {
            groups.Add(sessions);
        }

        var projects = SearchProjects(normalizedQuery, source.Projects);
        cancellationToken.ThrowIfCancellationRequested();
        if (projects.Items.Length > 0)
        {
            groups.Add(projects);
        }

        var settings = SearchSettings(normalizedQuery);
        cancellationToken.ThrowIfCancellationRequested();
        if (settings.Items.Length > 0)
        {
            groups.Add(settings);
        }

        var commands = SearchCommands(normalizedQuery);
        cancellationToken.ThrowIfCancellationRequested();
        if (commands.Items.Length > 0)
        {
            groups.Add(commands);
        }

        var snapshot = new GlobalSearchSnapshot(
            groups
                .OrderByDescending(group => group.Priority)
                .ToImmutableArray());
        return Task.FromResult(snapshot);
    }

    private GlobalSearchGroupSnapshot SearchSessions(string normalizedQuery, ImmutableArray<GlobalSearchSessionSource> sessions)
    {
        var matches = sessions
            .Select(session => new
            {
                Session = session,
                Score = Math.Max(
                    MatchScore(session.Title, normalizedQuery),
                    MatchScore(session.ConversationId, normalizedQuery))
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Take(10)
            .Select(item => new GlobalSearchItemSnapshot(
                item.Session.ConversationId,
                item.Session.Title,
                item.Session.Cwd,
                SearchResultKind.Session,
                "\uE8BD",
                Tag: null))
            .ToImmutableArray();

        return new GlobalSearchGroupSnapshot(
            Name: "sessions",
            Title: ResolveLocalizedValue("Search_Sessions", "Sessions"),
            Priority: 100,
            Items: matches);
    }

    private GlobalSearchGroupSnapshot SearchProjects(string normalizedQuery, ImmutableArray<GlobalSearchProjectSource> projects)
    {
        var items = new List<GlobalSearchItemSnapshot>();
        var unclassifiedTitle = ResolveLocalizedValue("Nav_Unclassified", NavigationProjectIds.Unclassified);
        if (MatchScore(unclassifiedTitle, normalizedQuery) > 0)
        {
            items.Add(new GlobalSearchItemSnapshot(
                NavigationProjectIds.Unclassified,
                unclassifiedTitle,
                Subtitle: null,
                SearchResultKind.Project,
                "\uE8F1",
                Tag: null));
        }

        foreach (var project in projects)
        {
            if (MatchScore(project.Name, normalizedQuery) <= 0
                && MatchScore(project.RootPath, normalizedQuery) <= 0)
            {
                continue;
            }

            items.Add(new GlobalSearchItemSnapshot(
                project.ProjectId,
                project.Name,
                project.RootPath,
                SearchResultKind.Project,
                "\uE8F1",
                Tag: null));
        }

        return new GlobalSearchGroupSnapshot(
            Name: "projects",
            Title: ResolveLocalizedValue("Search_Projects", "Projects"),
            Priority: 90,
            Items: items.ToImmutableArray());
    }

    private GlobalSearchGroupSnapshot SearchSettings(string normalizedQuery)
    {
        var settingsItems = new (string Id, string TitleResourceKey, string SubtitleResourceKey)[]
        {
            (SettingsSectionCatalog.GeneralKey, "SettingsSection_General", "SettingsSearchSubtitle_General"),
            (SettingsSectionCatalog.ShortcutsKey, "SettingsSection_Shortcuts", "SettingsSearchSubtitle_Shortcuts"),
            (SettingsSectionCatalog.AppearanceKey, "SettingsSection_Appearance", "SettingsSearchSubtitle_Appearance"),
            (SettingsSectionCatalog.DataStorageKey, "SettingsSection_DataStorage", "SettingsSearchSubtitle_DataStorage"),
            (SettingsSectionCatalog.AgentAcpKey, "SettingsSection_AgentAcp", "SettingsSearchSubtitle_AgentAcp"),
            (SettingsSectionCatalog.DiagnosticsKey, "SettingsSection_Diagnostics", "SettingsSearchSubtitle_Diagnostics"),
            (SettingsSectionCatalog.AboutKey, "SettingsSection_About", "SettingsSearchSubtitle_About")
        };

        var items = settingsItems
            .Select(item => new
            {
                item.Id,
                Title = ResolveLocalizedValue(item.TitleResourceKey, item.Id),
                Subtitle = ResolveLocalizedValue(item.SubtitleResourceKey, string.Empty)
            })
            .Where(item =>
                MatchScore(item.Title, normalizedQuery) > 0
                || MatchScore(item.Subtitle, normalizedQuery) > 0
                || MatchScore(item.Id, normalizedQuery) > 0)
            .Select(item => new GlobalSearchItemSnapshot(
                item.Id,
                item.Title,
                item.Subtitle,
                SearchResultKind.Setting,
                "\uE713",
                Tag: null))
            .ToImmutableArray();

        return new GlobalSearchGroupSnapshot(
            Name: "settings",
            Title: ResolveLocalizedValue("Search_Settings", "Settings"),
            Priority: 80,
            Items: items);
    }

    private string ResolveLocalizedValue(string resourceKey, string fallback)
    {
        var localized = _localizer[resourceKey];
        return localized is null || string.IsNullOrWhiteSpace(localized.Value) || localized.ResourceNotFound
            ? fallback
            : localized.Value;
    }

    private GlobalSearchGroupSnapshot SearchCommands(string normalizedQuery)
    {
        var commands = new (string Id, string TitleResourceKey, string TitleFallback, string SubtitleResourceKey, string SubtitleFallback, string Tag)[]
        {
            ("new_session", "SearchCommand_NewSessionTitle", "New session", "SearchCommand_NewSessionSubtitle", "Create a new chat session", "new"),
            ("new_project", "SearchCommand_NewProjectTitle", "New project", "SearchCommand_NewProjectSubtitle", "Add a project folder", "project"),
            ("toggle_theme", "SearchCommand_ToggleThemeTitle", "Toggle theme", "SearchCommand_ToggleThemeSubtitle", "Switch between light, dark, and system theme", "theme")
        };

        var items = commands
            .Select(command => new
            {
                command.Id,
                Title = ResolveLocalizedValue(command.TitleResourceKey, command.TitleFallback),
                Subtitle = ResolveLocalizedValue(command.SubtitleResourceKey, command.SubtitleFallback),
                command.Tag
            })
            .Where(command =>
                MatchScore(command.Title, normalizedQuery) > 0
                || MatchScore(command.Subtitle, normalizedQuery) > 0
                || MatchScore(command.Id, normalizedQuery) > 0)
            .Select(command => new GlobalSearchItemSnapshot(
                command.Id,
                command.Title,
                command.Subtitle,
                SearchResultKind.Command,
                "\uE756",
                command.Tag))
            .ToImmutableArray();

        return new GlobalSearchGroupSnapshot(
            Name: "commands",
            Title: ResolveLocalizedValue("Search_Commands", "Commands"),
            Priority: 70,
            Items: items);
    }

    private static int MatchScore(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var lower = text.ToLowerInvariant();
        if (lower == query)
        {
            return 100;
        }

        if (lower.StartsWith(query, StringComparison.Ordinal))
        {
            return 80;
        }

        if (lower.Contains(query, StringComparison.Ordinal))
        {
            return 50;
        }

        var words = lower.Split(new[] { ' ', '_', '-', '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => word.StartsWith(query, StringComparison.Ordinal)) ? 60 : 0;
    }
}
