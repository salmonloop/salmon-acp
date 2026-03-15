namespace SalmonEgg.Presentation.Utilities;

public sealed class InitialScrollGate
{
    private bool _pending = true;
    private bool _inFlight;

    public bool TrySchedule(int itemCount)
    {
        if (!_pending || _inFlight || itemCount <= 0)
        {
            return false;
        }

        _inFlight = true;
        return true;
    }

    public bool TryComplete(int itemCount)
    {
        _inFlight = false;

        if (!_pending || itemCount <= 0)
        {
            return false;
        }

        _pending = false;
        return true;
    }

    public void MarkPending()
    {
        _pending = true;
    }

    public void CancelInFlight()
    {
        _inFlight = false;
    }
}
