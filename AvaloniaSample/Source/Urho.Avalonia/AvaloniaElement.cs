using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Urho.AvaloniaAdapter;
using Urho.Gui;


namespace Urho
{
    public class AvaloniaElement : Sprite
    {
        private UrhoTopLevelImpl _windowImpl;

        public AvaloniaElement(Context context) : base(context)
        {
            SetEnabledRecursive(true);
            this.Enabled = true;
            this.Visible = true;
    
            FocusMode = FocusMode.Focusable;

            this.Resized += OnResized;
            this.DragMove += OnDragMove;
            this.Click += OnClickBegin;
            this.ClickEnd += OnClickEnd;

        }

        private void OnClickEnd(ClickEndEventArgs e)
        {
            if (e.Element != this)
                return;
            var screenPos = this.ScreenPosition;
            var position = new IntVector2(e.X - screenPos.X, e.Y - screenPos.Y);
            switch ((MouseButton)e.Button)
            {
                case MouseButton.Left:
                    _inputModifiers.Set(RawInputModifiers.LeftMouseButton);
                    SendRawEvent(RawPointerEventType.LeftButtonUp, position);
                    break;
                case MouseButton.Middle:
                    _inputModifiers.Set(RawInputModifiers.MiddleMouseButton);
                    SendRawEvent(RawPointerEventType.MiddleButtonUp, position);
                    break;
                case MouseButton.Right:
                    _inputModifiers.Set(RawInputModifiers.RightMouseButton);
                    SendRawEvent(RawPointerEventType.RightButtonUp, position);
                    break;
                case MouseButton.X1:
                    _inputModifiers.Set(RawInputModifiers.XButton1MouseButton);
                    SendRawEvent(RawPointerEventType.XButton1Up, position);
                    break;
                case MouseButton.X2:
                    _inputModifiers.Set(RawInputModifiers.XButton2MouseButton);
                    SendRawEvent(RawPointerEventType.XButton2Up, position);
                    break;
            }
        }

        private void OnClickBegin(ClickEventArgs e)
        {
            if (e.Element != this)
                return;
            var screenPos = this.ScreenPosition;
            var position = new IntVector2(e.X - screenPos.X, e.Y - screenPos.Y);
            switch ((MouseButton)e.Button)
            {
                case MouseButton.Left:
                    _inputModifiers.Set(RawInputModifiers.LeftMouseButton);
                    SendRawEvent(RawPointerEventType.LeftButtonDown, position);
                    break;
                case MouseButton.Middle:
                    _inputModifiers.Set(RawInputModifiers.MiddleMouseButton);
                    SendRawEvent(RawPointerEventType.MiddleButtonDown, position);
                    break;
                case MouseButton.Right:
                    _inputModifiers.Set(RawInputModifiers.RightMouseButton);
                    SendRawEvent(RawPointerEventType.RightButtonDown, position);
                    break;
                case MouseButton.X1:
                    _inputModifiers.Set(RawInputModifiers.XButton1MouseButton);
                    SendRawEvent(RawPointerEventType.XButton1Down, position);
                    break;
                case MouseButton.X2:
                    _inputModifiers.Set(RawInputModifiers.XButton2MouseButton);
                    SendRawEvent(RawPointerEventType.XButton2Down, position);
                    break;
            }
        }

        private void OnDragMove(DragMoveEventArgs e)
        {
            if (e.Element != this)
                return;
            var screenPos = this.ScreenPosition;
            var position = new IntVector2(e.X - screenPos.X, e.Y - screenPos.Y);
            SendRawEvent(RawPointerEventType.Move, position);
        }

        private void OnResized(ResizedEventArgs obj)
        {
         
            var size = this.Size;
            var clientSize = new Avalonia.Size(size.X / _windowImpl.RenderScaling, size.Y / _windowImpl.RenderScaling);
            if (_windowImpl.ClientSize != clientSize)
            {
                _windowImpl.Resize(clientSize);
            }
            ImageRect = new IntRect(0, 0, size.X, size.Y);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public UrhoTopLevelImpl Canvas
        {
            get
            {
                return _windowImpl;
            }
            set
            {
                if (_windowImpl != value)
                {
                    _windowImpl = value;
                    if (_windowImpl != null)
                    {
                        var size = _windowImpl.VisibleSize;
                        ImageRect = new IntRect(0, 0, size.X, size.Y);
                        Size = size;
                        Texture = _windowImpl.Texture;
                        var pointToScreen = _windowImpl.PointToScreen(new Point(0, 0));
                        Position = new IntVector2(pointToScreen.X, pointToScreen.Y);
                    }
                }
            }
        }

        public override void Update(float timeStep)
        {
            base.Update(timeStep);
        }

        

        private void SendRawEvent(RawPointerEventType type, IntVector2 position)
        {
            if (_windowImpl != null)
            {
                _windowImpl.Input(new RawPointerEventArgs(_windowImpl.MouseDevice, (ulong) DateTimeOffset.UtcNow.Ticks,
                    _windowImpl.InputRoot, type, new Point(position.X, position.Y),
                    _inputModifiers.Modifiers));
            }
        }

        private InputModifiersContainer _inputModifiers = new InputModifiersContainer();
    }
}