using System;

namespace FotoCreaDB.Wpf.Adapters;

/// <summary>
/// One pending value per operation. Report never posts work to a dispatcher or
/// synchronization context; the UI samples the latest value on its own timer.
/// </summary>
internal sealed class CoalescingProgress<T> : IProgress<T> where T : class
{
    private readonly object _gate = new();
    private T? _latest;
    private bool _completed;

    public void Report(T value)
    {
        if (value == null) return;
        lock (_gate)
        {
            if (!_completed) _latest = value;
        }
    }

    public T? TakeLatest()
    {
        lock (_gate)
        {
            T? latest = _latest;
            _latest = null;
            return latest;
        }
    }

    public T? Complete()
    {
        lock (_gate)
        {
            _completed = true;
            T? latest = _latest;
            _latest = null;
            return latest;
        }
    }
}
