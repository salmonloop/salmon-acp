using System;
using System.IO;
using System.Threading.Tasks;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class LogStreamService : ILogStreamService
{
    public IDisposable? StartWatching(string filePath, Action<string> onContentChanged)
    {
#if __WASM__
        return null;
#else
        if (string.IsNullOrWhiteSpace(filePath) || onContentChanged == null)
        {
            return null;
        }

        if (!File.Exists(filePath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        FileSystemEventHandler handler = (_, e) =>
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                try
                {
                    var newContent = File.ReadAllText(e.FullPath);
                    onContentChanged(newContent);
                }
                catch
                {
                }
            });
        };

        watcher.Changed += handler;
        watcher.EnableRaisingEvents = true;
        return new WatchSubscription(watcher, handler);
#endif
    }

#if !__WASM__
    private sealed class WatchSubscription : IDisposable
    {
        private FileSystemWatcher? _watcher;
        private FileSystemEventHandler? _handler;

        public WatchSubscription(FileSystemWatcher watcher, FileSystemEventHandler handler)
        {
            _watcher = watcher;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_watcher == null)
            {
                return;
            }

            try
            {
                if (_handler != null)
                {
                    _watcher.Changed -= _handler;
                }
            }
            catch
            {
            }

            try
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
            catch
            {
            }

            _watcher = null;
            _handler = null;
        }
    }
#endif
}
