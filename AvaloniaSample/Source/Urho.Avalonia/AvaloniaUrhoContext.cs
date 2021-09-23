using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Urho.Avalonia;
using Urho.Gui;
using Urho.IO;

using AvaloniaWindow=Avalonia.Controls.Window;

namespace Urho.Avalonia
{
    public class AvaloniaUrhoContext
    {
        private readonly object _windowsCollectionLock = new object();
        private readonly HashSet<UrhoTopLevelImpl> _windows = new HashSet<UrhoTopLevelImpl>();

        readonly HashSet<UrhoTopLevelImpl> _windowsToPaint = new HashSet<UrhoTopLevelImpl>();

        public double RenderScaling {get;set;} = 1.0;

        public static OperatingSystemType OperatingSystemType;

        public static Timer GlobalTimer;

        public ManagedDeferredRendererLock DeferredRendererLock = new ManagedDeferredRendererLock();

        public static CursorShape CursorShape = CursorShape.Normal;
        public static AvaloniaWindow MainWindow {
            get;
            set;
            }

        public static AvaloniaUrhoContext Instance = null;
        public AvaloniaUrhoContext(Context context)
        {
            Context = context;
            MouseDevice = new MouseDevice();
            KeyboardDevice = new KeyboardDevice();
            Screen = new UrhoScreenStub(this);

            Application.Current.Update += ProcessWindows;
            Application.Current.Input.ExitRequested += OnExitRequested;

            Platform = Application.Platform;
            GlobalTimer = new Timer();

            Instance = this;
          
        }

        private void ProcessWindows(UpdateEventArgs obj)
        {
            if (_windowsToPaint.Count > 0)
            {
                var windowsToPaint = _windowsToPaint.ToList();
                _windowsToPaint.Clear();
                foreach (var window in windowsToPaint)
                {
                    window.PaintImpl();
                }
            }
        }

        private void OnExitRequested(ExitRequestedEventArgs obj)
        {
       
        }



        public Context Context { get; }
        
        public IMouseDevice MouseDevice { get; }

        public IKeyboardDevice KeyboardDevice {get;}

        public IScreenImpl Screen { get; }

        public Urho.Platforms  Platform {get;}

        public void SchedulePaint(UrhoTopLevelImpl window)
        {
            lock (_windowsCollectionLock)
            {
                if (_windows.Contains(window))
                {
                    _windowsToPaint.Add(window);
                }
            }
        }

        public void RepaintAllWindows()
        {
            lock (_windowsCollectionLock)
            {
                foreach (var window in _windows)
                {
                    _windowsToPaint.Add(window);
                }
            }
        }

        public static void EnsureInvokeOnMainThread(Action action)
        {
            if (!Application.isExiting)
            {
                try
                {
                    Application.InvokeOnMain(action);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }   
            }
        }


        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        public void AddWindow(UrhoTopLevelImpl window)
        {
            lock (_windowsCollectionLock)
            {
                _windows.Add(window);
            }
        }

        internal void RemoveWindow(UrhoTopLevelImpl window)
        {
            lock (_windowsCollectionLock)
            {
                _windows.Remove(window);
            }
        }

    

    }
}
