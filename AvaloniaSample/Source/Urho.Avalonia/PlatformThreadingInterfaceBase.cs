using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Urho.Avalonia
{
    public abstract class PlatformThreadingInterfaceBase : IPlatformThreadingInterface
    {
        public event Action<DispatcherPriority?> Signaled;

        public static AvaloniaUrhoContext _context;

        public virtual bool CurrentThreadIsLoopThread => true;

        public abstract void EnsureInvokeOnMainThread(Action action);

        public abstract void RunLoop(CancellationToken cancellationToken);

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            if (interval.TotalMilliseconds < 10)
                interval = TimeSpan.FromMilliseconds(10);
            var l = new object();
            var stopped = false;
            System.Threading.Timer timer = null;
            var scheduled = false;
            timer = new System.Threading.Timer(_ =>
            {
                lock (l)
                {
                    if (stopped)
                    {
                        timer.Dispose();
                        return;
                    }

                    if (scheduled)
                        return;
                    scheduled = true;
                    
                    EnsureInvokeOnMainThread(() =>
                    {
                        using (var l = _context.DeferredRendererLock.Lock())
                        {
                            try
                            {
                                tick();
                            }
                            finally
                            {
                                lock (l)
                                {
                                    scheduled = false;
                                }
                            }
                        }
                    });
                }
            }, null, TimeSpan.Zero, interval);

            return Disposable.Create(() =>
            {
                lock (l)
                {
                    stopped = true;
                    timer.Dispose();
                }
            });
        }

        public void Signal(DispatcherPriority prio)
        {
            EnsureInvokeOnMainThread(() =>
            {
                using (var l = _context.DeferredRendererLock.Lock())
                {
                    Signaled?.Invoke(null);
                }
            });

        }
    }
}