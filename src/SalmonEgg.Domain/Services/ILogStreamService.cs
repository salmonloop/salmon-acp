using System;

namespace SalmonEgg.Domain.Services;

public interface ILogStreamService
{
    IDisposable? StartWatching(string filePath, Action<string> onContentChanged);
}
