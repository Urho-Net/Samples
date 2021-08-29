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

namespace Urho.Avalonia
{
    public class UrhoTopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
    {
        private PixelPoint _position;
        public  AvaloniaElement _urhoUIElement = new AvaloniaElement(Application.CurrentContext);

        public AvaloniaElement UrhoUIElement
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
            Dpi = 96.0;
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
                    _dpi = value;
                    if (_framebufferSource != null) _framebufferSource.Dpi = new Vector(_dpi, _dpi);
                    if (_hasActualSize) Resize(clientSize);
                    ScalingChanged?.Invoke(RenderScaling);
                    //Invalidate(new Rect(new Point(0,0), ClientSize));
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
                    UrhoUIElement.Position = new IntVector2(_position.X, _position.Y);
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
            get => _dpi / 96.0;
            set
            {
                var scaling = RenderScaling;
                if (scaling != value) Dpi = 96.0 * value;
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

        public virtual IMouseDevice MouseDevice => UrhoContext.MouseDevice;

        public Action<RawInputEventArgs> Input 
        { 
            get; 
            set; 
        }
        public Action<global::Avalonia.Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }
        public Action Closed { get; set; }
        public Action LostFocus { get; set; }
        public WindowTransparencyLevel TransparencyLevel { get; } = WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
            new AcrylicPlatformCompensationLevels(1, 1, 1);

        public virtual void Resize(Size clientSize)
        {
            var scaling = RenderScaling;
            var pixelSize = new PixelSize((int)(clientSize.Width * scaling), (int)(clientSize.Height * scaling));
            _hasActualSize = true;
            FramebufferSource.Size = pixelSize;
            FireResizedIfNecessary();
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

        private TextureFramebufferSource CreateFramebuffer()
        {
            _framebufferSource = new TextureFramebufferSource(UrhoContext);

            var parentUiElement = ParentUIElement;
            var element = new AvaloniaElement(UrhoContext.Context);
            element.Canvas = this;
            element.SetParent(parentUiElement);
            this.UrhoUIElement = element;
            
            return _framebufferSource;
            
        }

        public virtual void Dispose()
        {
            UrhoContext.RemoveWindow(this);
            _framebufferSource?.Dispose();
            _urhoUIElement.Dispose();
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

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new ImmediateRenderer(root);
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
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
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
                //_hasUpdatedImage = true;
            }
        }

        private void SchedulePaint()
        {
            UrhoContext.SchedulePaint(this);
        }

        private void FireResizedIfNecessary()
        {
            var size = ClientSize;
            if (_clientSizeCache != size)
            {
                _clientSizeCache = size;
                Resized?.Invoke(size);
            }

            if (UrhoUIElement != null)
            {
                UrhoUIElement.Size = VisibleSize;
            }
        }
    }
}