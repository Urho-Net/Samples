using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Platform;
using Urho.IO;

namespace Urho.Avalonia
{
    public class UrhoWindowImpl : UrhoTopLevelImpl, IWindowImpl  
    {
        private WindowState _windowState;

        SystemDecorations _systemDecorations = SystemDecorations.Full;

        public UrhoWindowImpl(AvaloniaUrhoContext avaloniaUrhoContext) : base(avaloniaUrhoContext)
        {
             RenderScaling = avaloniaUrhoContext.RenderScaling;
             UrhoUIElement.Priority = 100;
             
        }


        public virtual void Show(bool activate)
        {
            //TBD ELI
            if (UrhoUIElement.Parent == null)
            {
                UrhoUIElement.SetParent(Application.Current.UI.Root);
            }

            if (_systemDecorations == SystemDecorations.Full)
            {
                UrhoUIElement.CreateTitleBar();
            }
            
            UrhoUIElement.SetFocus(true);
            UrhoUIElement.BringToFront();
        }

        public virtual void Show(bool activate, bool isDialog)
        {
            if (UrhoUIElement.Parent == null)
            {
                UrhoUIElement.SetParent(Application.Current.UI.Root);
            }

            if (_systemDecorations == SystemDecorations.Full)
            {
                UrhoUIElement.CreateTitleBar();
            }

            UrhoUIElement.SetFocus(true);
            UrhoUIElement.BringToFront();
        }

        public virtual void Hide()
        {
        }

        public virtual void Activate()
        {
            Activated?.Invoke();
        }

        public virtual void SetTopmost(bool value)
        {
        }

        public virtual double DesktopScaling { get; }
        

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public IPlatformHandle Handle { get; }
        public Size MaxAutoSizeHint { get; }
        public IScreenImpl Screen => UrhoContext.Screen;
        
        public virtual void SetTitle(string title)
        {
            UrhoUIElement.SetTitle(title);    
        }

        public virtual void SetParent(IWindowImpl parent)
        {
            var urhoParentWindowImpl = parent as UrhoWindowImpl;
            if (urhoParentWindowImpl != null && urhoParentWindowImpl.UrhoUIElement != null)
            {
                UrhoUIElement.SetParent(urhoParentWindowImpl.UrhoUIElement);
                Invalidate();
            }  
        }

        public virtual void SetEnabled(bool enable)
        {
        }

        public virtual void SetSystemDecorations(SystemDecorations enabled)
        {
            _systemDecorations = enabled;

            if(_systemDecorations == SystemDecorations.None)
            {
                UrhoUIElement.DeleteTitleBar();
            }
            else if (_systemDecorations == SystemDecorations.Full)
            {
                UrhoUIElement.CreateTitleBar();
            }
        }

        public virtual void SetIcon(IWindowIconImpl icon)
        {
        }

        public virtual void ShowTaskbarIcon(bool value)
        {
        }

        public virtual void CanResize(bool value)
        {
            UrhoUIElement.Resizable = value;
        }

        public virtual void BeginMoveDrag(PointerPressedEventArgs e)
        {
            UrhoUIElement.BeginMoveDrag();
        }

        public virtual void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
        }


        public virtual void Move(PixelPoint point)
        {
            Position = point;
        }

        public virtual void SetMinMaxSize(Size minSize, Size maxSize)
        {
            UrhoUIElement.minSize = minSize;
            UrhoUIElement.maxSize = maxSize;
        }

        public virtual void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
        }

        public virtual void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
        }

        public virtual void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
        }


        public virtual WindowState WindowState
        {
            get => _windowState;
            set
            {
                if (_windowState != value)
                {
                    _windowState = value;
                    if (_windowState == WindowState.Maximized)
                    {
                        IsFullscreen = true;
                        UrhoUIElement.MaximizeWindow(true);
                    }
                    else if (_windowState == WindowState.Normal)
                    {
                        IsFullscreen = false;
                        UrhoUIElement.MaximizeWindow(false);
                    }
                    else{
                        IsFullscreen = false;
                    }
                    WindowStateChanged?.Invoke(_windowState);
                }
            }
        }

        public bool IsFullscreen { get; private set; }

        public Action<WindowState> WindowStateChanged { get; set; }
        public Action GotInputWhenDisabled { get; set; }
        public Func<bool> Closing 
        { 
            get; 
            set; 
            }
        public bool IsClientAreaExtendedToDecorations { get; }
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
        public bool NeedsManagedDecorations { get; }
        public Thickness ExtendedMargins { get; }
        public Thickness OffScreenMargin { get; }
    }
}