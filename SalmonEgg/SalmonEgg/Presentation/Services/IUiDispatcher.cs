using System;

namespace SalmonEgg.Presentation.Services;

public interface IUiDispatcher
{
    bool TryEnqueue(Action action);
}
