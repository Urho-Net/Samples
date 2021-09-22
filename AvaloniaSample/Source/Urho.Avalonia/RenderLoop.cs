using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.Rendering;
using System.Reflection;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using Urho;
using Urho.IO;

namespace Urho.Avalonia
{
    /// <summary>
    /// The application render loop.
    /// </summary>
    /// <remarks>
    /// The render loop is responsible for advancing the animation timer and updating the scene
    /// graph for visible windows.
    /// </remarks>
    public class RenderLoop : IRenderLoop
    {

        AvaloniaUrhoContext _avaloniaUrhoContext;
        private List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();
        private IRenderTimer _timer;
        private int _inTick;
        private int _inUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        public RenderLoop(AvaloniaUrhoContext avaloniaUrhoContext)
        {
            _avaloniaUrhoContext = avaloniaUrhoContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        /// <param name="timer">The render timer.</param>
        /// <param name="dispatcher">The UI thread dispatcher.</param>
        public RenderLoop(IRenderTimer timer, IDispatcher dispatcher)
        {
            _timer = timer;
        }


        /// <summary>
        /// Gets the render timer.
        /// </summary>
        protected IRenderTimer Timer
        {
            get
            {
                if (_timer == null)
                {
                    _timer = AvaloniaLocator.Current.GetService<IRenderTimer>();
                }

                return _timer;
            }
        }

        /// <inheritdoc/>
        public void Add(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();

            using (var l = _avaloniaUrhoContext.DeferredRendererLock.Lock())
            {
                _items.Add(i);

                if (_items.Count == 1)
                {
                    Timer.Tick += TimerTick;
                }
            }
        }

        /// <inheritdoc/>
        public void Remove(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();
            using (var l = _avaloniaUrhoContext.DeferredRendererLock.Lock())
            {
                _items.Remove(i);

                if (_items.Count == 0)
                {
                    Timer.Tick -= TimerTick;
                }
            }
        }

        private void TimerTick(TimeSpan time)
        {
            using (var l = _avaloniaUrhoContext.DeferredRendererLock.Lock())
            {
                if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
                {
                    try
                    {
                        bool needsUpdate = false;

                        foreach (IRenderLoopTask item in _items)
                        {
                            if (item.NeedsUpdate)
                            {
                                needsUpdate = true;

                                break;
                            }
                        }

                        if (needsUpdate &&
                            Interlocked.CompareExchange(ref _inUpdate, 1, 0) == 0)
                        {

                            AvaloniaUrhoContext.EnsureInvokeOnMainThread(() =>
                            {
                                using (var l = _avaloniaUrhoContext.DeferredRendererLock.Lock())
                                {
                                    for (var i = 0; i < _items.Count; ++i)
                                    {
                                        var item = _items[i];

                                        if (item.NeedsUpdate)
                                        {
                                            try
                                            {
                                                item.Update(time);
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render update: {Error}", ex);
                                            }
                                        }
                                    }

                                    Interlocked.Exchange(ref _inUpdate, 0);
                                }
                            });
                        }

                        try
                        {
                            using (var l2 = _avaloniaUrhoContext.DeferredRendererLock.Lock())
                            {
                                _itemsCopy.Clear();
                                foreach (var i in _items)
                                    _itemsCopy.Add(i);


                                for (int i = 0; i < _itemsCopy.Count; i++)
                                {
                                    _itemsCopy[i].Render();
                                }

                                _itemsCopy.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception in render loop:" + ex.ToString());
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _inTick, 0);
                    }
                }
            }
        }
    }
}
