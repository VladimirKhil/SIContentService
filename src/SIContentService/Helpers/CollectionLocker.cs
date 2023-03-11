namespace SIContentService.Helpers;

/// <summary>
/// Allows to use a dynamic set of locks specified by keys.
/// </summary>
internal sealed class CollectionLocker
{
    private readonly object _packageLock = new();

    private readonly HashSet<string> _packageLocks = new();

    internal async Task DoWithLockAsync(string lockKey, Action action, CancellationToken cancellationToken = default)
    {
        bool locked;

        do
        {
            lock (_packageLock)
            {
                locked = _packageLocks.Contains(lockKey);

                if (!locked)
                {
                    _packageLocks.Add(lockKey);
                }
            }

            if (locked)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            try
            {
                action();
                return;
            }
            finally
            {
                lock (_packageLock)
                {
                    _packageLocks.Remove(lockKey);
                }
            }
        } while (true);
    }

    internal async Task<T> DoWithLockAsync<T>(string lockKey, Func<T> action, CancellationToken cancellationToken = default)
    {
        bool locked;

        do
        {
            lock (_packageLock)
            {
                locked = _packageLocks.Contains(lockKey);

                if (!locked)
                {
                    _packageLocks.Add(lockKey);
                }
            }

            if (locked)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            try
            {
                return action();
            }
            finally
            {
                lock (_packageLock)
                {
                    _packageLocks.Remove(lockKey);
                }
            }
        } while (true);
    }

    internal async Task DoAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default)
    {
        bool locked;

        do
        {
            lock (_packageLock)
            {
                locked = _packageLocks.Contains(lockKey);

                if (!locked)
                {
                    _packageLocks.Add(lockKey);
                }
            }

            if (locked)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            try
            {
                await action();
                return;
            }
            finally
            {
                lock (_packageLock)
                {
                    _packageLocks.Remove(lockKey);
                }
            }
        } while (true);
    }
}
