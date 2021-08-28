// TBD ELI 
// #define MANAGED_BUFFER

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using SkiaSharp;

using Urho.Urho2D;
using Urho;

namespace Urho.AvoloniaAdapter
{
    public class TextureFramebufferSource: IDisposable
    {
        private readonly AvaloniaUrhoContext _avaloniaContext;
        private LockedFramebuffer _lockedFramebuffer;
#if MANAGED_BUFFER
        private byte[] _data = Array.Empty<byte>();
#else
        private UnmanagedArray _data = UnmanagedArray.Empty;
#endif
        private readonly Texture2D _texture;
        private PixelSize _size;
        private TextureUsage _textureUsage = TextureUsage.Dynamic;

        public Texture2D Texture => _texture;
        private bool _externalTexture;

        public bool RenderToExternalTexture => _externalTexture;

        public TextureFramebufferSource(AvaloniaUrhoContext avaloniaContext, Texture2D targetTexture = null)
        {
            _avaloniaContext = avaloniaContext;
            if (targetTexture != null)
            {
                _texture = targetTexture;
                _externalTexture = true;
            }
            else
            {
                _texture = new Texture2D(avaloniaContext.Context);
                _externalTexture = false;
            }
            _texture.SetNumLevels(1);
            _lockedFramebuffer = new LockedFramebuffer(this);
            switch (SKImageInfo.PlatformColorType)
            {
                //case SKColorType.Bgra8888:
                //    Format = PixelFormat.Bgra8888;
                //    break;
                default:
                    Format = PixelFormat.Rgba8888;
                    break;
            }

        }

        public ILockedFramebuffer Lock()
        {
            _lockedFramebuffer.Lock();
            return _lockedFramebuffer;
        }

        public PixelSize Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;

                    if (value.Width == 0 || value.Height == 0)
                    {
                        return;
                        //throw new ArgumentOutOfRangeException("Size can't be zero");
                    }

       
                    var width = MathHelper.NextPowerOfTwo(_size.Width);
                    var height = MathHelper.NextPowerOfTwo(_size.Height);
                    var texture2D = Texture;
                    if (width != texture2D.Width || height != texture2D.Height)
                    {
                        RowBytes = width * 4;
                        if (RowBytes * height > _data.Length)
                        {
#if MANAGED_BUFFER
                            _data = new byte[RowBytes * height];
#else
                            _data?.Dispose();
                            _data = new UnmanagedArray(RowBytes * height);
#endif
                        }

                        if (!texture2D.SetSize(width, height, GetFormat(Format), _textureUsage))
                        {
                            throw new InvalidOperationException("Can't resize texture");
                        }
                    }
                }
            }
        }

        private uint GetFormat(PixelFormat format)
        {
           
            switch (format)
            {
                case PixelFormat.Rgba8888:
                    return  Graphics.RGBAFormat;
                default:
                    throw new NotImplementedException(format.ToString());
            }
        }

        class LockedFramebuffer: ILockedFramebuffer
        {
            private readonly TextureFramebufferSource _source;
#if MANAGED_BUFFER
            private GCHandle _pinnedArray;
#endif

            public LockedFramebuffer(TextureFramebufferSource source)
            {
                _source = source;
            }

            public void Dispose()
            {
                // TBD ELI 
               var arr =  MarshalHelper.ToBytesArray(Address,_source._data.Length);

                var texture = _source.Texture;
                texture.SetData(0, 0, 0, texture.Width, texture.Height, arr);
#if MANAGED_BUFFER
                _pinnedArray.Free();
                _pinnedArray = default;
#endif
            }

            public IntPtr Address { 
                get; 
                private set; 
                }

            public PixelSize Size => _source.Size;

            public int RowBytes => _source.RowBytes;
            
            public Vector Dpi => _source.Dpi;
            
            public PixelFormat Format => _source.Format;

            public void Lock()
            {
#if MANAGED_BUFFER
                _pinnedArray = GCHandle.Alloc(_source._data, GCHandleType.Pinned);
                Address = _pinnedArray.AddrOfPinnedObject();
#else
                Address = _source._data.Addr;
#endif
            }
        }

        public int RowBytes { get; private set; }

        public PixelFormat Format { get; }

        public Vector Dpi { get; set; }

        public void Dispose()
        {
#if MANAGED_BUFFER
#else
            _data?.Dispose();
#endif
            _texture.Dispose();
        }
    }
}