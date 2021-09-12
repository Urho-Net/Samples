using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Visuals;
using Urho.Gui;
using Urho.IO;

using AvaloniaKey = Avalonia.Input.Key;
using UrhoKey = Urho.Key;

namespace Urho.Avalonia
{
    public class AvaloniaElement : Sprite
    {
        private UrhoTopLevelImpl _windowImpl;
        
       Input UrhoInput = null ;

       float WheelX = 0.0f;
       float WheelY = 0.0f;

       float WHEEL_DECAY_STEP = 50.0f;

        public AvaloniaElement(Context context) : base(context)
        {
            SetEnabledRecursive(true);
            this.Enabled = true;
            this.Visible = true;

            UrhoInput = Application.Current.Input;
    
            FocusMode = FocusMode.Focusable;

            this.Resized += OnResized;
            // this.DragMove += OnDragMove;
            this.Click += OnClickBegin;
            this.ClickEnd += OnClickEnd;
            Application.Current.Input.KeyDown += OnKeyDown;
            Application.Current.Input.KeyUp += OnKeyUp;
            Application.Current.Input.TextInput += OnTextInputEvent;
            Application.Current.Input.MouseMoved += OnMouseMove;
            Application.Current.Input.MouseWheel += OnMouseWheel;

            Application.Current.Engine.PostUpdate += OnPostUpdate;
    
        }

        private void OnPostUpdate(PostUpdateEventArgs evt)
        {
            if (!this.HasFocus()) return;
            RawInputModifiers modifiers = RawInputModifiers.None;

            if(MathF.Abs(WheelX) > 1.0f || MathF.Abs(WheelY) > 1.0f)
            {
                UpdateInputModifiers(ref modifiers);
                WheelX = MathHelper.Lerp(WheelX,0.0f,0.1f*evt.TimeStep*WHEEL_DECAY_STEP);
                WheelY = MathHelper.Lerp(WheelY,0.0f,0.1f*evt.TimeStep*WHEEL_DECAY_STEP);
                SendMouseWheelEvent(WheelX,WheelY,modifiers);
                // Log.Info(WheelY.ToString());
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
                    // this.DragMove -= OnDragMove;
                    this.Click -= OnClickBegin;
                    this.ClickEnd -= OnClickEnd;
                    Application.Current.Input.KeyDown -= OnKeyDown;
                    Application.Current.Input.KeyUp -= OnKeyUp;
                    Application.Current.Input.TextInput -= OnTextInputEvent;
                    Application.Current.Input.MouseMoved -= OnMouseMove;
                    Application.Current.Input.MouseWheel -= OnMouseWheel;
                    Application.Current.Engine.PostUpdate -= OnPostUpdate;
                }
           
            }
            catch (Exception ex)
            {

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

        // private void OnDragMove(DragMoveEventArgs e)
        // {
        //     if (e.Element != this)
        //         return;
        //     var screenPos = this.ScreenPosition;
        //     var position = new Vector2(e.X - screenPos.X, e.Y - screenPos.Y);
        //     SendRawPointerEvent(RawPointerEventType.Move, position);
        // }

        private void OnResized(ResizedEventArgs obj)
        {
         
            var size = this.Size;
            var clientSize = new global::Avalonia.Size(size.X / _windowImpl.RenderScaling, size.Y / _windowImpl.RenderScaling);
            if (_windowImpl.ClientSize != clientSize)
            {
                _windowImpl.Resize(clientSize);
            }
            ImageRect = new IntRect(0, 0, size.X, size.Y);
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

                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Left)) ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Right)) ? RawInputModifiers.RightMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Middle)) ? RawInputModifiers.MiddleMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X1)) ? RawInputModifiers.XButton1MouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X2)) ? RawInputModifiers.XButton2MouseButton : RawInputModifiers.None;

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
               
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Left)) ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Right)) ? RawInputModifiers.RightMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.Middle)) ? RawInputModifiers.MiddleMouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X1)) ? RawInputModifiers.XButton1MouseButton : RawInputModifiers.None;
                modifiers |= (UrhoInput.GetMouseButtonDown(MouseButton.X2)) ? RawInputModifiers.XButton2MouseButton : RawInputModifiers.None;

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

            IntVector2 mousePosition = UrhoInput.MousePosition;
            Vector2 position = new Vector2((mousePosition.X) / (float)_windowImpl.RenderScaling, (mousePosition.Y) / (float)_windowImpl.RenderScaling);
            Vector vector = new Vector(wheel_x * -0.1, wheel_y * 0.1);

            var args = new RawMouseWheelEventArgs(_windowImpl.MouseDevice, (ulong)AvaloniaUrhoContext.GlobalTimer.GetMSec(false), _windowImpl.InputRoot, new Point(position.X, position.Y), vector, modifiers);
            _windowImpl.Input?.Invoke(args);
        }

        private InputModifiersContainer _inputModifiers = new InputModifiersContainer();
    }
}