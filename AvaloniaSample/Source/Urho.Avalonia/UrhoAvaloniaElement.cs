using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Visuals;
using Urho.Gui;
using Urho.IO;

using AvaloniaKey = Avalonia.Input.Key;
using UrhoKey = Urho.Key;

namespace Urho.Avalonia
{
    public class UrhoAvaloniaElement : Sprite
    {
        public UrhoTopLevelImpl _windowImpl;
        public Size minSize ;
        public Size maxSize;
        
       Input UrhoInput = null ;

       float WheelX = 0.0f;
       float WheelY = 0.0f;

       float WHEEL_DECAY_STEP = 25.0f;

        IntVector2 dragBeginCursor_ = IntVector2.Zero;
        IntVector2 dragBeginPosition_ = IntVector2.Zero;

         IntVector2 dragBeginSize_ = IntVector2.Zero;

        IntRect resizeBorder_ = new IntRect(10, 10, 10, 10);
        WindowDragMode dragMode_ = WindowDragMode.None;
        WindowDragMode hoverMode_ = WindowDragMode.None;

        bool Movable = false;
        public bool Resizable = true;

        public WindowTitleBar windowTitleBar = null;

        string windowTitle = string.Empty;

        bool isMaximized = false;
        IntVector2 previousPosition;
        IntVector2 previousSize;

        IntVector2 ApplicationGraphicsSize =  new IntVector2() ;

        bool AvloniaBeginDrag = false;

        public UrhoAvaloniaElement(Context context) : base(context)
        {
            SetEnabledRecursive(true);
            this.Enabled = true;
            this.Visible = true;

            UrhoInput = Application.Current.Input;
    
            FocusMode = FocusMode.Focusable;

            this.Resized += OnResized;
            this.DragBegin += OnDragBegin;
            this.DragMove += OnDragMove;
            this.DragEnd += OnDragEnd;
            this.Click += OnClickBegin;
            this.ClickEnd += OnClickEnd;
            this.BringToFrontOnFocus =true;
            this.Focused += OnFocus;
            this.Defocused += OnDefocused;

            Application.Current.Input.KeyDown += OnKeyDown;
            Application.Current.Input.KeyUp += OnKeyUp;
            Application.Current.Input.TextInput += OnTextInputEvent;
            Application.Current.Input.MouseMoved += OnMouseMove;
            Application.Current.Input.MouseWheel += OnMouseWheel;

            Application.Current.Engine.PostUpdate += OnPostUpdate;

            ApplicationGraphicsSize = Urho.Application.Current.Graphics.Size;

        }

        private void OnDefocused(DefocusedEventArgs obj)
        {
            Priority = 100;

            _windowImpl.LostFocus?.Invoke();
        }

        private void OnFocus(FocusedEventArgs obj)
        {
            Priority = 101;
        }

        public void MaximizeWindow(bool maximize)
        {
            if(Resizable == false )return;

            if (maximize == true && isMaximized == false)
            {
                isMaximized = true;

               var window = (_windowImpl as UrhoWindowImpl );
               window.WindowState = WindowState.Maximized;

                previousPosition = Position;
                previousSize = Size;
                if (windowTitleBar != null)
                {
                    this.SetPosition(-ScreenPosition.X + Position.X, -ScreenPosition.Y + Position.Y + windowTitleBar.Height);
                    this.Size = new IntVector2(Application.Current.Graphics.Width, Application.Current.Graphics.Height - windowTitleBar.Height);
                }
                else
                {
                    this.SetPosition(-ScreenPosition.X + Position.X, -ScreenPosition.Y + Position.Y);
                    this.Size = new IntVector2(Application.Current.Graphics.Width, Application.Current.Graphics.Height);
                }
            }
            else if (maximize == false && isMaximized == true)
            {
                isMaximized = false;
                var window = (_windowImpl as UrhoWindowImpl);
                window.WindowState = WindowState.Normal;
                this.SetPosition(previousPosition.X, previousPosition.Y);
                this.Size = previousSize;
            }
        }

      
        public void ToggleMaximizeWindow()
        {
            if(Resizable == false )return;

            isMaximized = ! isMaximized;

            if(isMaximized == true)
            {
                var window = (_windowImpl as UrhoWindowImpl);
                window.WindowState = WindowState.Maximized;

                previousPosition = Position;
                previousSize = Size;

                this.SetPosition( -ScreenPosition.X+Position.X,-ScreenPosition.Y+Position.Y+windowTitleBar.Height);
                this.Size = new IntVector2(Application.Current.Graphics.Width,Application.Current.Graphics.Height-windowTitleBar.Height);
            }
            else
            {
                var window = (_windowImpl as UrhoWindowImpl);
                window.WindowState = WindowState.Normal;

                this.SetPosition(previousPosition.X,previousPosition.Y);
                this.Size = previousSize;
            }

        }

        public void SetTitle(string title)
        {
            if (windowTitleBar == null)
            {
                CreateTitleBar();
            }

            windowTitle = title;
            windowTitleBar.windowTitle.Text = windowTitle;

        }
        public void CreateTitleBar()
        {
            if (windowTitleBar == null)
            {
                windowTitleBar = new WindowTitleBar(this);
                AddChild(windowTitleBar);
                windowTitleBar.Width = this.Width;

                this.SetPosition(Position.X,Position.Y + windowTitleBar.Height);

                if (windowTitle != string.Empty)
                {
                    windowTitleBar.windowTitle.Text = windowTitle;
                }

                Movable = false;

                if(isMaximized == true)
                {
                    previousPosition.Y += windowTitleBar.Height;

                    this.SetSize(Width,Height-windowTitleBar.Height);
                }

            }
        }

        public void DeleteTitleBar()
        {
            if (windowTitleBar != null)
            {
                if (isMaximized == true)
                {
                    previousPosition.Y -= windowTitleBar.Height;
                    this.SetSize(Width,Height+windowTitleBar.Height);
                }

                this.SetPosition(Position.X,Position.Y - windowTitleBar.Height);
                windowTitleBar.Dispose();
                windowTitleBar = null;

                Movable = false;

            }
        }

        public void SetAvaloniaPosition(int x ,int y)
        {

            if (windowTitleBar != null)
            {
                Position = new IntVector2(x, y + windowTitleBar.Height);
            }
            else{
                Position = new IntVector2(x, y);
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try
            {
                if(!Application.isExiting)
                {
                    this.Resized -= OnResized;
                    this.DragBegin -= OnDragBegin;
                    this.DragMove -= OnDragMove;
                    this.DragEnd -= OnDragEnd;
                    this.Click -= OnClickBegin;
                    this.ClickEnd -= OnClickEnd;
                    this.Focused -= OnFocus;
                    this.Defocused -= OnDefocused;

                    Application.Current.Input.KeyDown -= OnKeyDown;
                    Application.Current.Input.KeyUp -= OnKeyUp;
                    Application.Current.Input.TextInput -= OnTextInputEvent;
                    Application.Current.Input.MouseMoved -= OnMouseMove;
                    Application.Current.Input.MouseWheel -= OnMouseWheel;
                    Application.Current.Engine.PostUpdate -= OnPostUpdate;

                    if (windowTitleBar != null)
                    {
                        windowTitleBar.Dispose();
                        windowTitleBar = null;
                    }
                }
           
            }
            catch (Exception ex)
            {

            }
        }

        private void OnPostUpdate(PostUpdateEventArgs evt)
        {

            if (ApplicationGraphicsSize != Urho.Application.Current.Graphics.Size)
            {
                ApplicationGraphicsSize = Urho.Application.Current.Graphics.Size;

                if (windowTitleBar != null)
                {
                    DeleteTitleBar();
                    CreateTitleBar();
                }

                CursorFactory.ResetCursorFactory();
            }

            if (Application.Current.UI.Cursor != null)
            {
                Application.Current.UI.Cursor.SetShape(AvaloniaUrhoContext.CursorShape);
            }


            if (!this.HasFocus()) return;

            RawInputModifiers modifiers = RawInputModifiers.None;

            if (MathF.Abs(WheelX) > 1.0f || MathF.Abs(WheelY) > 1.0f)
            {
                UpdateInputModifiers(ref modifiers);
                WheelX = MathHelper.Lerp(WheelX, 0.0f, 0.1f * evt.TimeStep * WHEEL_DECAY_STEP);
                WheelY = MathHelper.Lerp(WheelY, 0.0f, 0.1f * evt.TimeStep * WHEEL_DECAY_STEP);
                SendMouseWheelEvent(WheelX, WheelY, modifiers);
                // Log.Info(WheelY.ToString());
            }

        }


        private void OnClickBegin(ClickEventArgs e)
        {
            if (e.Element != this)
                return;
            var screenPos = this.ScreenPosition;
            var position = new Vector2(e.X - screenPos.X, e.Y - screenPos.Y);
            // var position = new Vector2((e.X - screenPos.X) / (float)_windowImpl.RenderScaling, (e.Y - screenPos.Y) / (float)_windowImpl.RenderScaling);
            
            //  Log.Info("" + screenPos + " " + position);
            WheelX = 0.0f;
            WheelY = 0.0f;

            switch ((MouseButton)e.Button)
            {
                case MouseButton.Left:
                    _inputModifiers.Set(RawInputModifiers.LeftMouseButton);
                    SendRawPointerEvent(RawPointerEventType.LeftButtonDown, position);
                    break;
                case MouseButton.Middle:
                    _inputModifiers.Set(RawInputModifiers.MiddleMouseButton);
                    SendRawPointerEvent(RawPointerEventType.MiddleButtonDown, position);
                    break;
                case MouseButton.Right:
                    _inputModifiers.Set(RawInputModifiers.RightMouseButton);
                    SendRawPointerEvent(RawPointerEventType.RightButtonDown, position);
                    break;
                case MouseButton.X1:
                    _inputModifiers.Set(RawInputModifiers.XButton1MouseButton);
                    SendRawPointerEvent(RawPointerEventType.XButton1Down, position);
                    break;
                case MouseButton.X2:
                    _inputModifiers.Set(RawInputModifiers.XButton2MouseButton);
                    SendRawPointerEvent(RawPointerEventType.XButton2Down, position);
                    break;
            }
        }

        private void OnClickEnd(ClickEndEventArgs e)
        {
            if (e.Element != this)
                return;
            var screenPos = this.ScreenPosition;
            var position = new Vector2(e.X - screenPos.X, e.Y - screenPos.Y);
            switch ((MouseButton)e.Button)
            {
                case MouseButton.Left:
                    _inputModifiers.Set(RawInputModifiers.LeftMouseButton);
                    SendRawPointerEvent(RawPointerEventType.LeftButtonUp, position);
                    break;
                case MouseButton.Middle:
                    _inputModifiers.Set(RawInputModifiers.MiddleMouseButton);
                    SendRawPointerEvent(RawPointerEventType.MiddleButtonUp, position);
                    break;
                case MouseButton.Right:
                    _inputModifiers.Set(RawInputModifiers.RightMouseButton);
                    SendRawPointerEvent(RawPointerEventType.RightButtonUp, position);
                    break;
                case MouseButton.X1:
                    _inputModifiers.Set(RawInputModifiers.XButton1MouseButton);
                    SendRawPointerEvent(RawPointerEventType.XButton1Up, position);
                    break;
                case MouseButton.X2:
                    _inputModifiers.Set(RawInputModifiers.XButton2MouseButton);
                    SendRawPointerEvent(RawPointerEventType.XButton2Up, position);
                    break;
            }
        }


        private void OnMouseMove(MouseMovedEventArgs e)
        {
            
            if (!this.HasFocus()) return;
            var screenPos = this.ScreenPosition;
            var position = new Vector2(e.X - screenPos.X, e.Y - screenPos.Y);
            SendRawPointerEvent(RawPointerEventType.Move, position);

            var hoverMode = GetDragMode();
            if (hoverMode == WindowDragMode.ResizeBottom)
            {
                CursorFactory.SetCursor(StandardCursorType.SizeNorthSouth);
            }
            else if (hoverMode == WindowDragMode.ResizeBottomRight)
            {
                CursorFactory.SetCursor(StandardCursorType.BottomRightCorner);
            }
            else if (hoverMode == WindowDragMode.ResizeRight)
            {
                CursorFactory.SetCursor(StandardCursorType.SizeWestEast);
            }
            else
            {
                if (hoverMode_ != hoverMode)
                {
                    CursorFactory.SetCursor(StandardCursorType.Arrow);
                }
            }

            hoverMode_ = hoverMode;

        }

        private void UpdateInputModifiers(ref RawInputModifiers modifiers )
        {

            if (UrhoInput.GetKeyDown(UrhoKey.Shift) || UrhoInput.GetKeyDown(UrhoKey.Rshift))
            {
                modifiers |= RawInputModifiers.Shift;
            }
            
            if (UrhoInput.GetKeyDown(UrhoKey.Ctrl) || UrhoInput.GetKeyDown(UrhoKey.Rctrl))
            {
                modifiers |= RawInputModifiers.Control;
            }
             
            if (UrhoInput.GetKeyDown(UrhoKey.Alt) || UrhoInput.GetKeyDown(UrhoKey.Ralt))
            {
                modifiers |= RawInputModifiers.Alt;
            }

            if (_windowImpl.Platform == Platforms.MacOSX)
            {
                if (UrhoInput.GetKeyDown(UrhoKey.Gui) || UrhoInput.GetKeyDown(UrhoKey.Rgui))
                {
                    modifiers |= RawInputModifiers.Control;
                }
            }

            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Left)) ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Right)) ? RawInputModifiers.RightMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Middle)) ? RawInputModifiers.MiddleMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X1)) ? RawInputModifiers.XButton1MouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X2)) ? RawInputModifiers.XButton2MouseButton : RawInputModifiers.None;

        }

        private void OnMouseWheel(MouseWheelEventArgs evt)
        {
            if (!this.HasFocus()) return;
         
           RawInputModifiers modifiers = RawInputModifiers.None;

            if ((evt.Qualifiers & 1) != 0)
            {
                modifiers |= RawInputModifiers.Shift;
            }
            if ((evt.Qualifiers & 2) != 0)
            {
                modifiers |= RawInputModifiers.Control;
            }
            if ((evt.Qualifiers & 4) != 0)
            {
                modifiers |= RawInputModifiers.Alt;
            }

            if (_windowImpl.Platform == Platforms.MacOSX)
            {
                if (UrhoInput.GetKeyDown(UrhoKey.Gui) || UrhoInput.GetKeyDown(UrhoKey.Rgui))
                {
                    modifiers |= RawInputModifiers.Control;
                }
            }

            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Left)) ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Right)) ? RawInputModifiers.RightMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Middle)) ? RawInputModifiers.MiddleMouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X1)) ? RawInputModifiers.XButton1MouseButton : RawInputModifiers.None;
            modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X2)) ? RawInputModifiers.XButton2MouseButton : RawInputModifiers.None;

            this.WheelX = evt.WheelX;
            this.WheelY = evt.WheelY;
            SendMouseWheelEvent(evt.WheelX,evt.WheelY,modifiers);

            // Log.Info("" + evt.WheelX + " " + evt.WheelY);

        }

        private WindowDragMode GetDragMode()
        {
            WindowDragMode mode = WindowDragMode.None;

            IntVector2 mousePosition = UrhoInput.MousePosition - ScreenPosition;
            // Log.Info(mousePosition.ToString() + ":" +  this.Size);

            if (Movable == true && mousePosition.Y < resizeBorder_.Top)
            {
                mode = WindowDragMode.Move;
            }
            else if(Resizable == true && mousePosition.Y  >= Size.Y - resizeBorder_.Bottom &&  mousePosition.Y  < Size.Y )
            {
                mode = WindowDragMode.ResizeBottom;

                if (mousePosition.X >= Width - resizeBorder_.Right &&  mousePosition.X < Width)
                {
                    mode = WindowDragMode.ResizeBottomRight;
                }
            }
            else if (Resizable == true && mousePosition.X >= Width - resizeBorder_.Right &&  mousePosition.X < Width )
            {
                    mode = WindowDragMode.ResizeRight;
            }
            else if(AvloniaBeginDrag == true)
            {
                mode = WindowDragMode.Move;
            }

            return mode;
        }

        public void OnWindowTitleDragBegin(DragBeginEventArgs e)
        {
            dragBeginCursor_ = new IntVector2(e.X, e.Y);
            dragBeginPosition_ = Position;
        }

        public void OnWindowTitleDragMove(DragMoveEventArgs e)
        {
            IntVector2 delta = new IntVector2(e.X - dragBeginCursor_.X, e.Y - dragBeginCursor_.Y);
            Position = dragBeginPosition_ + delta;
        }
        public void OnWindowTitleDragEnd(DragEndEventArgs e)
        {

        }

        // this one is called from Avlonia if the user starts to drag a window
        public void BeginMoveDrag()
        {
            AvloniaBeginDrag = true;
        }

        private void OnDragBegin(DragBeginEventArgs e)
        {
            if (e.Element != this)
                return;

            
            dragBeginCursor_ = new IntVector2(e.X, e.Y);
            dragBeginPosition_ = Position;

            dragBeginSize_ = Size;

            dragMode_ = GetDragMode();

        }

        private void OnDragMove(DragMoveEventArgs e)
        {
            if (e.Element != this)
                return;
            if (dragMode_ == WindowDragMode.Move)
            {
                IntVector2 delta = new IntVector2(e.X - dragBeginCursor_.X, e.Y - dragBeginCursor_.Y);
                Position = dragBeginPosition_ + delta;
            }
            else if(dragMode_ == WindowDragMode.ResizeBottom)
            {
                  IntVector2 delta = new IntVector2(e.X - dragBeginCursor_.X, e.Y - dragBeginCursor_.Y);
                  this.SetSize(dragBeginSize_.X,dragBeginSize_.Y + delta.Y);

                  CursorFactory.SetCursor(StandardCursorType.SizeNorthSouth);
            }
            else if(dragMode_ == WindowDragMode.ResizeBottomRight)
            {
                IntVector2 delta = new IntVector2(e.X - dragBeginCursor_.X, e.Y - dragBeginCursor_.Y);
                this.SetSize(dragBeginSize_.X + delta.X, dragBeginSize_.Y + delta.Y);

                CursorFactory.SetCursor(StandardCursorType.BottomRightCorner);
            }
            else if (dragMode_ == WindowDragMode.ResizeRight)
            {
                IntVector2 delta = new IntVector2(e.X - dragBeginCursor_.X, e.Y - dragBeginCursor_.Y);
                this.SetSize(dragBeginSize_.X + delta.X, dragBeginSize_.Y);

                 CursorFactory.SetCursor(StandardCursorType.SizeWestEast);
            }
        }

        private void OnDragEnd(DragEndEventArgs e)
        {

            AvloniaBeginDrag = false;
             
            CursorFactory.SetCursor(StandardCursorType.Arrow);

            if (e.Element != this)
                return;

            dragMode_ = WindowDragMode.None;
            var screenPos = this.ScreenPosition;
            var position = new Vector2(e.X - screenPos.X, e.Y - screenPos.Y);
            _inputModifiers.Set(RawInputModifiers.LeftMouseButton);
            SendRawPointerEvent(RawPointerEventType.LeftButtonUp, position);
        }

        private void OnResized(ResizedEventArgs obj)
        {
         
          
            var clientSize = new global::Avalonia.Size(Size.X / _windowImpl.RenderScaling, Size.Y / _windowImpl.RenderScaling);

            bool isClamped = false;

            if(minSize.Width != 0)
            {
                if (clientSize.Width < minSize.Width)
                {
                    clientSize = clientSize.WithWidth(minSize.Width);
                    isClamped = true;
                }
            }

            if (minSize.Height != 0)
            {
                if (clientSize.Height < minSize.Height)
                {
                    clientSize = clientSize.WithHeight(minSize.Height);
                    isClamped = true;
                }
            }

            if (maxSize.Width != 0)
            {
                if (clientSize.Width > maxSize.Width)
                {
                    clientSize = clientSize.WithWidth(maxSize.Width);
                    isClamped = true;
                }
            }

            if (maxSize.Height != 0)
            {
                if (clientSize.Height > maxSize.Height)
                {
                    clientSize = clientSize.WithHeight(maxSize.Height);
                    isClamped = true;
                }
            }

            if( isClamped == true)
            {
                Size = new IntVector2((int)(clientSize.Width * _windowImpl.RenderScaling), (int)(clientSize.Height * _windowImpl.RenderScaling));
            }

            if (_windowImpl.ClientSize != clientSize)
            {
                _windowImpl.Resize(clientSize,PlatformResizeReason.User);
            }
            ImageRect = new IntRect(0, 0, Size.X, Size.Y);

            if (windowTitleBar != null)
            {
                windowTitleBar.Width = Size.X;
            }
        }

        private void OnKeyDown(KeyDownEventArgs evt)
        {
            if (!this.HasFocus()) return;

            //  Log.Info("OnKeyDown:" + evt.Key.ToString()+" " + evt.Buttons + " " + evt.Qualifiers + " " + evt.Scancode + " " + evt.Repeat);

            RawInputModifiers modifiers = RawInputModifiers.None;

            if ((evt.Qualifiers & 1) != 0)
            {
                modifiers |= RawInputModifiers.Shift;
            }
            if ((evt.Qualifiers & 2) != 0)
            {
                modifiers |= RawInputModifiers.Control;
            }
            if ((evt.Qualifiers & 4) != 0)
            {
                modifiers |= RawInputModifiers.Alt;
            }

            if (_windowImpl.Platform == Platforms.MacOSX)
            {
                if (UrhoInput.GetKeyDown(UrhoKey.Gui) || UrhoInput.GetKeyDown(UrhoKey.Rgui))
                {
                    modifiers |= RawInputModifiers.Control;
                }
            }

           if(KeyTranslate.UrhoToAvaloniaKeyMapping.TryGetValue(evt.Key , out AvaloniaKey avnKey))
           {
                 SendRawKeyEvent(RawKeyEventType.KeyDown, avnKey, modifiers);
           }
            
        }

        private void OnKeyUp(KeyUpEventArgs evt)
        {
            if (!this.HasFocus()) return;
            // Log.Info("OnKeyUp:" + evt.Key.ToString()+" " + evt.Buttons + " " + evt.Qualifiers + " " + evt.Scancode);

            RawInputModifiers modifiers = RawInputModifiers.None;

            if ((evt.Qualifiers & 1) != 0)
            {
                modifiers |= RawInputModifiers.Shift;
            }
            if ((evt.Qualifiers & 2) != 0)
            {
                modifiers |= RawInputModifiers.Control;
            }
            if ((evt.Qualifiers & 4) != 0)
            {
                modifiers |= RawInputModifiers.Alt;
            }

            if (_windowImpl.Platform == Platforms.MacOSX)
            {
                if (UrhoInput.GetKeyDown(UrhoKey.Gui) || UrhoInput.GetKeyDown(UrhoKey.Rgui))
                {
                    modifiers |= RawInputModifiers.Control;
                }
            }
            
            if (KeyTranslate.UrhoToAvaloniaKeyMapping.TryGetValue(evt.Key, out AvaloniaKey avnKey))
            {
                SendRawKeyEvent(RawKeyEventType.KeyUp, AvaloniaKey.Enter, modifiers);
            }
        }

        private void OnTextInputEvent(TextInputEventArgs evt)
        {
            if (!this.HasFocus()) return;

            SendRawTextInputEvent(evt.Text);

            //   Log.Info("OnTextInputEvent :" + evt.Text);
    
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

        

        private void SendRawPointerEvent(RawPointerEventType type, Vector2 position)
        {
            if (_windowImpl != null)
            {

                
                 position = new Vector2((position.X) / (float)_windowImpl.RenderScaling, (position.Y) / (float)_windowImpl.RenderScaling);

              
                RawInputModifiers modifiers = RawInputModifiers.None;

                UpdateInputModifiers(ref  modifiers);

                var args = new RawPointerEventArgs(_windowImpl.MouseDevice, (ulong) AvaloniaUrhoContext.GlobalTimer.GetMSec(false),
                    _windowImpl.InputRoot, type, new Point(position.X, position.Y),
                    modifiers);

                _windowImpl.Input?.Invoke(args);
                
            }
        }

        private void SendRawKeyEvent(RawKeyEventType type, AvaloniaKey key, RawInputModifiers modifiers)
        {
            if (_windowImpl != null)
            {
               
                UpdateInputModifiers(ref  modifiers);

                var args = new RawKeyEventArgs(_windowImpl.KeyboardDevice, (ulong)AvaloniaUrhoContext.GlobalTimer.GetMSec(false), _windowImpl.InputRoot, type, key, modifiers);
                _windowImpl.Input?.Invoke(args);
            }
        }

        private void SendRawTextInputEvent(string text)
        {
            if (_windowImpl != null)
            {
                var args = new RawTextInputEventArgs(_windowImpl.KeyboardDevice, (ulong)AvaloniaUrhoContext.GlobalTimer.GetMSec(false), _windowImpl.InputRoot, text);
                _windowImpl.Input?.Invoke(args);
            }
        }

        private void SendMouseWheelEvent(float wheel_x,float wheel_y, RawInputModifiers modifiers)
        {

            var screenPos = this.ScreenPosition;
            var position = new Vector2((UrhoInput.MousePosition.X - screenPos.X)/(float)_windowImpl.RenderScaling, (UrhoInput.MousePosition.Y - screenPos.Y)/(float)_windowImpl.RenderScaling);

           
            Vector vector = new Vector(wheel_x * -0.1, wheel_y * 0.1);

            UpdateInputModifiers(ref modifiers);

            var args = new RawMouseWheelEventArgs(_windowImpl.MouseDevice, (ulong)AvaloniaUrhoContext.GlobalTimer.GetMSec(false), _windowImpl.InputRoot, new Point(position.X, position.Y), vector, modifiers);
            _windowImpl.Input?.Invoke(args);
        }

        private InputModifiersContainer _inputModifiers = new InputModifiersContainer();
    }
}