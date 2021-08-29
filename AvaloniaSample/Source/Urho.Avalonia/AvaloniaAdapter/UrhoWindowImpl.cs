using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

namespace Urho.AvaloniaAdapter
{
    public class UrhoWindowImpl : UrhoTopLevelImpl, IWindowImpl
    {
        private Size _minSize;
        private Size _maxSize;
        private WindowState _windowState;

        public UrhoWindowImpl(AvaloniaUrhoContext avaloniaUrhoContext) : base(avaloniaUrhoContext)
        {
        }

        public virtual void Show(bool activate)
        {
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
        }

        public virtual void SetParent(IWindowImpl parent)
        {
        }

        public virtual void SetEnabled(bool enable)
        {
        }

        public virtual void SetSystemDecorations(SystemDecorations enabled)
        {
        }

        public virtual void SetIcon(IWindowIconImpl icon)
        {
        }

        public virtual void ShowTaskbarIcon(bool value)
        {
        }

        public virtual void CanResize(bool value)
        {
        }

        public virtual void BeginMoveDrag(PointerPressedEventArgs e)
        {
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
            _minSize = minSize;
            _maxSize = maxSize;
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
                        IsFullscreen = true;
                    else
                        IsFullscreen = false;
                    WindowStateChanged?.Invoke(_windowState);
                }
            }
        }

        public bool IsFullscreen { get; private set; }

        public Action<WindowState> WindowStateChanged { get; set; }
        public Action GotInputWhenDisabled { get; set; }
        public Func<bool> Closing { get; set; }
        public bool IsClientAreaExtendedToDecorations { get; }
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
        public bool NeedsManagedDecorations { get; }
        public Thickness ExtendedMargins { get; }
        public Thickness OffScreenMargin { get; }
    }
}