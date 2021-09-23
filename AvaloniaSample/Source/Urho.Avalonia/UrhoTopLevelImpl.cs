using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Urho.Urho2D;
using Urho.Gui;
using Urho.IO;
using static Urho.Avalonia.CursorFactory;

namespace Urho.Avalonia
{
    public class UrhoTopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
    {
        private PixelPoint _position;
        private  UrhoAvaloniaElement _urhoUIElement = null; //new UrhoAvaloniaSprite(Application.CurrentContext);
        const double CONST_DPI_VALUE_96 = 96.0;
        public UrhoAvaloniaElement UrhoUIElement
        {
            get
            {
                return _urhoUIElement;
            }
            
            private set
            {
                _urhoUIElement = value;
            }
        }

        private global::Avalonia.Rect _invalidRegion = global::Avalonia.Rect.Empty;
        private double _dpi;
        private TextureFramebufferSource _framebufferSource;
        private bool _hasActualSize;
        private Size _clientSizeCache;


        public UrhoTopLevelImpl(AvaloniaUrhoContext context)
        {
            UrhoContext = context;
            UrhoContext.AddWindow(this);
            Dpi = CONST_DPI_VALUE_96;
            Invalidate(new global::Avalonia.Rect(0, 0, double.MaxValue, double.MaxValue));

        }

        public AvaloniaUrhoContext UrhoContext { get; set; }

        public IInputRoot InputRoot { get; private set; }

        public double Dpi
        {
            get => _dpi;
            set
            {
                if (_dpi != value)
                {
                    var clientSize = ClientSize;
                    // TBD ELI
                    // make sure the window created is valid in size , use default size incase of zero width or height
                    if (clientSize.Width == 0 || clientSize.Height == 0)
                    {
                        clientSize = new Size(1300, 700);
                       _hasActualSize = true;
                    }

                    _dpi = value;
                    if (_framebufferSource != null) _framebufferSource.Dpi = new Vector(_dpi, _dpi);
                    if (_hasActualSize) Resize(clientSize,PlatformResizeReason.DpiChange);
                    ScalingChanged?.Invoke(RenderScaling);
                }
            }
        }
        public Action<PixelPoint> PositionChanged { get; set; }

        public virtual PixelPoint Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    PositionChanged?.Invoke(_position);
                    UrhoUIElement.SetAvaloniaPosition(_position.X, _position.Y);
                }
            }
        }

        public Texture2D Texture => _framebufferSource?.Texture;
        public IntVector2 VisibleSize => new IntVector2(FramebufferSource.Size.Width, FramebufferSource.Size.Height);

        public bool TrySetTargetTexture(Texture2D texture)
        {
            if (texture == null)
                return false;
            if (_framebufferSource != null)
                return false;
            _framebufferSource = new TextureFramebufferSource(UrhoContext, texture);
            return true;
        }

        /// <summary>
        ///     Gets the client size of the toplevel.
        /// </summary>
        public virtual Size ClientSize
        {
            get
            {
                var framebufferSize = FramebufferSource.Size;
                return new Size(framebufferSize.Width / RenderScaling, framebufferSize.Height / RenderScaling);
            }
        }

        /// <summary>
        ///     Gets the scaling factor for the toplevel.
        /// </summary>
        public virtual double RenderScaling
        {
            get => _dpi / CONST_DPI_VALUE_96;
            set
            {
                var scaling = RenderScaling;
                if (scaling != value) Dpi = CONST_DPI_VALUE_96 * value;
            }
        }

        /// <summary>
        ///     The list of native platform's surfaces that can be consumed by rendering subsystems.
        /// </summary>
        /// <remarks>
        ///     Rendering platform will check that list and see if it can utilize one of them to output.
        ///     It should be enough to expose a native window handle via IPlatformHandle
        ///     and add support for framebuffer (even if it's emulated one) via IFramebufferPlatformSurface.
        ///     If you have some rendering platform that's tied to your particular windowing platform,
        ///     just expose some toolkit-specific object (e. g. Func&lt;Gdk.Drawable&gt; in case of GTK#+Cairo)
        /// </remarks>
        public virtual IEnumerable<object> Surfaces
        {
            get { yield return this; }
        }

        public virtual Urho.Platforms Platform => UrhoContext.Platform;
        public virtual IMouseDevice MouseDevice => UrhoContext.MouseDevice;
        public virtual IKeyboardDevice KeyboardDevice => UrhoContext.KeyboardDevice;
        public Action<RawInputEventArgs> Input 
        { 
            get; 
            set; 
        }
        public Action<global::Avalonia.Rect> Paint { get; set; }

        public Action<double> ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }
        public Action Closed { get; set; }
        public Action LostFocus { get; set; }
        public WindowTransparencyLevel TransparencyLevel { get; } = WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
            new AcrylicPlatformCompensationLevels(1, 1, 1);

  
        public virtual void Resize(Size clientSize, PlatformResizeReason reason = PlatformResizeReason.Application)
        {
            if (clientSize.Width == 0 || clientSize.Height == 0)
            {
                clientSize = new Size(1300, 700);
                Invalidate();
            }

            var scaling = RenderScaling;
            var pixelSize = new PixelSize((int)(clientSize.Width * scaling), (int)(clientSize.Height * scaling));
            _hasActualSize = true;
            FramebufferSource.Size = pixelSize;
            FireResizedIfNecessary(reason);

        }

        public TextureFramebufferSource FramebufferSource
        {
            get
            {
                return _framebufferSource ?? CreateFramebuffer();
            }
        }

        public UIElement ParentUIElement
        {
            get
            {
                if (_urhoUIElement != null)
                    return _urhoUIElement.Parent;
                return Application.Current.UI.Root;
            }
        }

        private Size? _frameSize = new Size(1300, 700);
        public Size? FrameSize
        {
            get
            {
                return _frameSize;
            }
            protected set
            {
                _frameSize = value;
            }
        }

        public Action<Size, PlatformResizeReason> Resized { get; set; }

     
        private TextureFramebufferSource CreateFramebuffer()
        {
            _framebufferSource = new TextureFramebufferSource(UrhoContext);

            var parentUiElement = ParentUIElement;
            var element = new UrhoAvaloniaElement(UrhoContext.Context);
            element.Canvas = this;
            element.SetParent(parentUiElement);
            this.UrhoUIElement = element;
            
            return _framebufferSource;
            
        }

        public virtual void Dispose()
        {

            _urhoUIElement.SetParent(null);
            _urhoUIElement.Visible = false;

            UrhoContext.RemoveWindow(this);
            _framebufferSource?.Dispose();

            _urhoUIElement.Dispose();
            _urhoUIElement = null;

            this.Closed?.Invoke();
        }

        public ILockedFramebuffer Lock()
        {
            return FramebufferSource.Lock();
        }

        /// <summary>Invalidates a rect on the toplevel.</summary>
        public virtual void Invalidate(global::Avalonia.Rect rect)
        {

            _invalidRegion = _invalidRegion.Union(rect);
            SchedulePaint();
        }

        public virtual void Invalidate()
        {
            _invalidRegion = new global::Avalonia.Rect(ClientSize);
            SchedulePaint();
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
         ;

            return new DeferredRenderer(root, loop,null,null,UrhoContext.DeferredRendererLock)  {
                     RenderOnlyOnRenderThread = true
                };

                // return new ImmediateRenderer(root);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public Point PointToClient(PixelPoint point)
        {
            var position = Position;
            return (point - position).ToPoint(RenderScaling);
        }

        public PixelPoint PointToScreen(Point point)
        {
            var position = Position;
            return PixelPoint.FromPoint(point, RenderScaling) + position;
        }

        public void SetCursor(ICursorImpl cursor)
        {
            if(cursor != null)
            {
                var cursorStub = cursor as CursorStub;
                CursorFactory.SetCursor(cursorStub._cursorType);
            }
            else{
                CursorFactory.SetCursor(StandardCursorType.Arrow);
            }
         
        }

        public IPopupImpl CreatePopup()
        {
           return null;
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
        }

        internal void PaintImpl()
        {
            var paint = Paint;
            if (paint == null)
                return;

            var updateTexture = _invalidRegion != global::Avalonia.Rect.Empty;
            if (updateTexture)
            {
                var paintArea = _invalidRegion.Intersect(new global::Avalonia.Rect(new Point(0, 0), ClientSize));
                _invalidRegion = global::Avalonia.Rect.Empty;
                if (paintArea.Width * paintArea.Height > 0)
                    paint?.Invoke(paintArea);
            }
        }

        private void SchedulePaint()
        {
            UrhoContext.SchedulePaint(this);
        }

        private void FireResizedIfNecessary(PlatformResizeReason reason = PlatformResizeReason.Application)
        {
            using (var l = UrhoContext.DeferredRendererLock.Lock())
            {
                var size = ClientSize;
                if (_clientSizeCache != size)
                {
                    _clientSizeCache = size;
                     Resized?.Invoke(size,reason);
                }

                if (UrhoUIElement != null)
                {
                    UrhoUIElement.Size = VisibleSize;
                }
            }
        }

    }
}