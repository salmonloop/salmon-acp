using System.Text;
using System.Text.Json;

namespace SalmonEgg.GuiTests.Windows;

internal sealed class GuiAppDataScope : IDisposable
{
    private const string AppDataRootEnvVar = "SALMONEGG_APPDATA_ROOT";
    private readonly string _appDataRoot;
    private readonly string _configDirectory;
    private readonly string _conversationsDirectory;
    private readonly string _appYamlPath;
    private readonly string _conversationsPath;
    private readonly byte[]? _originalAppYaml;
    private readonly byte[]? _originalConversations;
    private readonly bool _appYamlExisted;
    private readonly bool _conversationsExisted;
    private readonly string _projectRootPath;
    private readonly string? _previousGuiAppDataRootOverride;
    private bool _disposed;

    private GuiAppDataScope(
        string appDataRoot,
        string appYamlPath,
        string conversationsPath,
        byte[]? originalAppYaml,
        bool appYamlExisted,
        byte[]? originalConversations,
        bool conversationsExisted,
        string projectRootPath,
        string? previousGuiAppDataRootOverride)
    {
        _appDataRoot = appDataRoot;
        _configDirectory = Path.GetDirectoryName(appYamlPath)!;
        _conversationsDirectory = Path.GetDirectoryName(conversationsPath)!;
        _appYamlPath = appYamlPath;
        _conversationsPath = conversationsPath;
        _originalAppYaml = originalAppYaml;
        _appYamlExisted = appYamlExisted;
        _originalConversations = originalConversations;
        _conversationsExisted = conversationsExisted;
        _projectRootPath = projectRootPath;
        _previousGuiAppDataRootOverride = previousGuiAppDataRootOverride;
    }

    public static GuiAppDataScope CreateDeterministicLeftNavData(int sessionCount = 1, bool withContent = false)
    {
        if (sessionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionCount));
        }

        GuiTestGate.RequireEnabled();
        WindowsGuiAppSession.StopAllRunningInstances();

        var appDataRoot = ResolveAppDataRoot();
        var previousGuiAppDataRootOverride = Environment.GetEnvironmentVariable(AppDataRootEnvVar);
        Environment.SetEnvironmentVariable(AppDataRootEnvVar, appDataRoot);
        var appYamlPath = Path.Combine(appDataRoot, "config", "app.yaml");
        var conversationsPath = Path.Combine(appDataRoot, "conversations", "conversations.v1.json");
        var projectRootPath = Path.Combine(Path.GetTempPath(), "SalmonEgg.GuiTests", "project-1");

        var scope = new GuiAppDataScope(
            appDataRoot,
            appYamlPath,
            conversationsPath,
            File.Exists(appYamlPath) ? File.ReadAllBytes(appYamlPath) : null,
            File.Exists(appYamlPath),
            File.Exists(conversationsPath) ? File.ReadAllBytes(conversationsPath) : null,
            File.Exists(conversationsPath),
            projectRootPath,
            previousGuiAppDataRootOverride);

        scope.Seed(sessionCount, withContent);
        return scope;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        WindowsGuiAppSession.StopAllRunningInstances();
        RestoreFile(_appYamlPath, _originalAppYaml, _appYamlExisted);
        RestoreFile(_conversationsPath, _originalConversations, _conversationsExisted);
        DeleteDirectoryIfEmpty(_configDirectory);
        DeleteDirectoryIfEmpty(_conversationsDirectory);
        DeleteDirectoryIfEmpty(_appDataRoot);
        Environment.SetEnvironmentVariable(AppDataRootEnvVar, _previousGuiAppDataRootOverride);

        try
        {
            if (Directory.Exists(_projectRootPath))
            {
                Directory.Delete(_projectRootPath, recursive: true);
            }
        }
        catch
        {
        }
    }

    public string ReadBootLogTail(int lineCount = 20)
    {
        var bootLogPath = Path.Combine(_appDataRoot, "boot.log");
        if (!File.Exists(bootLogPath))
        {
            return "<boot.log missing>";
        }

        try
        {
            return string.Join(
                Environment.NewLine,
                File.ReadLines(bootLogPath)
                    .TakeLast(lineCount));
        }
        catch (Exception ex)
        {
            return $"<boot.log unreadable: {ex.Message}>";
        }
    }

    private void Seed(int sessionCount, bool withContent = false)
    {
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(_conversationsDirectory);
        Directory.CreateDirectory(_projectRootPath);

        File.WriteAllText(_appYamlPath, BuildAppYaml(_projectRootPath), Encoding.UTF8);
        File.WriteAllText(_conversationsPath, BuildConversationsJson(_projectRootPath, sessionCount, withContent), Encoding.UTF8);
    }

    private static string ResolveAppDataRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable(AppDataRootEnvVar);
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            return overrideRoot.Trim();
        }

        return Path.Combine(
            Path.GetTempPath(),
            "SalmonEgg.GuiTests",
            "appdata",
            Guid.NewGuid().ToString("N"));
    }

    private static string BuildAppYaml(string projectRootPath)
    {
        var normalizedPath = projectRootPath.Replace("'", "''", StringComparison.Ordinal);

        return string.Join(
            Environment.NewLine,
            "schema_version: 1",
            "theme: System",
            "is_animation_enabled: true",
            "backdrop: System",
            "projects:",
            "  - project_id: project-1",
            "    name: GUI Project",
            $"    root_path: '{normalizedPath}'",
            "last_selected_project_id: project-1",
            string.Empty);
    }

    private static string BuildConversationsJson(string projectRootPath, int sessionCount, bool withContent = false)
    {
        var baseTime = new DateTimeOffset(2026, 03, 19, 09, 00, 00, TimeSpan.Zero);
        var conversations = Enumerable.Range(1, sessionCount)
            .Select(index =>
            {
                var timestamp = baseTime.AddMinutes(-(index - 1));
                object[] messages = Array.Empty<object>();
                
                if (withContent)
                {
                    messages = new object[]
                    {
                        new {
                            id = $"m-{index}-1",
                            timestamp = timestamp,
                            contentType = "text",
                            textContent = $"Hello from session {index}",
                            isOutgoing = false
                        },
                        new {
                            id = $"m-{index}-2",
                            timestamp = timestamp.AddSeconds(1),
                            contentType = "text",
                            textContent = $"Hi there!",
                            isOutgoing = true
                        }
                    };
                }

                return new
                {
                    conversationId = $"gui-session-{index:00}",
                    displayName = $"GUI Session {index:00}",
                    createdAt = timestamp,
                    lastUpdatedAt = timestamp,
                    cwd = projectRootPath,
                    messages = messages
                };
            })
            .ToArray();

        var document = new
        {
            version = 1,
            lastActiveConversationId = (string?)null,
            conversations
        };

        return JsonSerializer.Serialize(document);
    }

    private static void RestoreFile(string path, byte[]? content, bool existed)
    {
        try
        {
            if (existed && content != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, content);
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void DeleteDirectoryIfEmpty(string path)
    {
        try
        {
            if (Directory.Exists(path) &&
                !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path, recursive: false);
            }
        }
        catch
        {
        }
    }
}
