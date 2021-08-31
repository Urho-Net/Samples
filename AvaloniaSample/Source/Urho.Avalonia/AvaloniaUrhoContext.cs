using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Input;
using Avalonia.Platform;
using Urho.Avalonia;


namespace Urho.Avalonia
{
    public class AvaloniaUrhoContext
    {
        private readonly object _windowsCollectionLock = new object();
        private readonly HashSet<UrhoTopLevelImpl> _windows = new HashSet<UrhoTopLevelImpl>();

        readonly HashSet<UrhoTopLevelImpl> _windowsToPaint = new HashSet<UrhoTopLevelImpl>();

        public AvaloniaUrhoContext(Context context)
        {
            Context = context;
            MouseDevice = new MouseDevice();
            KeyboardDevice = new KeyboardDevice();
            Screen = new UrhoScreenStub(this);

            Application.Current.Update += ProcessWindows;
            Application.Current.Input.ExitRequested += OnExitRequested;

            Platform = Application.Platform;
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
                _windowsToPaint.Add(window);
            }
        }

        public void EnsureInvokeOnMainThread(Action action)
        {

        
           
            if (!Application.isExiting)
            {
                try
                {
                    Application.InvokeOnMain(action);
                }
                catch (Exception ex)
                {

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
