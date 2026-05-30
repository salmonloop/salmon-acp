using System.Text;

namespace SalmonEgg.GuiTests.Windows;

public sealed class TestFileIoTests : IDisposable
{
    private readonly string _tempRoot;

    public TestFileIoTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "salmonegg-gui-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task WriteAllTextWithRetry_Succeeds_WhenAnExclusiveLockIsReleasedShortlyAfterward()
    {
        var path = Path.Combine(_tempRoot, "app.yaml");
        await File.WriteAllTextAsync(path, "before", Encoding.UTF8);

        using var heldOpen = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var releaseTask = Task.Run(async () =>
        {
            await Task.Delay(120);
            heldOpen.Dispose();
        });

        TestFileIo.WriteAllTextWithRetry(path, "after", Encoding.UTF8, attempts: 6, delayMilliseconds: 50);
        await releaseTask;

        Assert.Equal("after", File.ReadAllText(path, Encoding.UTF8));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
        }
    }
}
