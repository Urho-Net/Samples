// TBD ELI 
#define MANAGED_BUFFER

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using SkiaSharp;

using Urho.Urho2D;
using Urho;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Urho.IO;

namespace Urho.Avalonia
{
    class LockedFramebuffer : ILockedFramebuffer
    {
        private readonly TextureFramebufferSource _source;
#if MANAGED_BUFFER
        private GCHandle _pinnedArray;
#endif

        public LockedFramebuffer(TextureFramebufferSource source)
        {
            _source = source;
        }

        public void Lock()
        {
            using (var l = _source._avaloniaContext.DeferredRendererLock.Lock())
            {
#if MANAGED_BUFFER
                byte[] data = null;
                int countAttempt = 0;
                while (data == null)
                {
                    countAttempt++;
                    data = _source.AllocData();
                    if (data != null)
                    {
                        _pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
                        Address = _pinnedArray.AddrOfPinnedObject();
                    }

                    if (countAttempt > 1)
                        Log.Error("LockedFramebuffer::Lock()  AllocData countAttempt:" + countAttempt);
                }
#else
                     Address = _source._data.Addr;
#endif

            }
        }

        public void Dispose()
        {
             using (var l = _source._avaloniaContext.DeferredRendererLock.Lock())
             {
#if MANAGED_BUFFER
                var obj = _pinnedArray.Target;
                _source.DisposeData((byte[])obj);
                _pinnedArray.Free();
                _pinnedArray = default;
#else
                var arr = MarshalHelper.ToBytesArray(Address, _source._data.Length);
                _source.DisposeData(arr);
#endif
             }
        }

        public IntPtr Address
        {
            get;
            private set;
        }

        public PixelSize Size => _source.Size;

        public int RowBytes => _source.RowBytes;

        public Vector Dpi => _source.Dpi;

        public PixelFormat Format => _source.Format;

    }





    public class TextureFramebufferSource : IDisposable
    {
        public readonly AvaloniaUrhoContext _avaloniaContext;
        private LockedFramebuffer _lockedFramebuffer;
#if MANAGED_BUFFER
        private ConcurrentQueue<byte[]> _freeByteArrayPool = new ConcurrentQueue<byte[]>() ;
        const int FREE_BYTE_ARRAY_POOL_SIZE = 2;
        public byte[] AllocData()
        {

            if (_freeByteArrayPool.TryDequeue(out byte[] data))
            {
                return data;
            }

            return null;
        }
#else
        public UnmanagedArray _data = UnmanagedArray.Empty;
#endif

        private ConcurrentQueue<byte[]> _disposedByteArrayPool = new ConcurrentQueue<byte[]>() ;

        public void DisposeData(byte[] data)
        {
            _disposedByteArrayPool.Enqueue(data);
        }
        
        private readonly Texture2D _texture;
        private PixelSize _size;

        int Width {get;set;} = 0;
        int Height {get;set;} = 0;

        int Length {get;set;} = 0;

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

            Application.Current.Update += OnUpdate;

        }

        private void OnUpdate(UpdateEventArgs obj)
        {

            if (_disposedByteArrayPool.TryDequeue(out byte[] data))
            {
                _texture.SetData(0, 0, 0, _texture.Width, _texture.Height, data);
#if MANAGED_BUFFER
                _freeByteArrayPool.Enqueue(data);
#endif
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

                    if (value.Width <= 0 || value.Height <= 0)
                    {
                        return;
                        //throw new ArgumentOutOfRangeException("Size can't be zero");
                    }

       
                    Width = MathHelper.NextPowerOfTwo(_size.Width);
                    Height = MathHelper.NextPowerOfTwo(_size.Height);
                    var texture2D = Texture;
                    if (Width != texture2D.Width || Height != texture2D.Height)
                    {
                        RowBytes = Width * 4;
                        using (var l = _avaloniaContext.DeferredRendererLock.Lock())
                        {
                            if (RowBytes * Height > Length)
                            {
#if MANAGED_BUFFER
                                Length = RowBytes * Height;
                                _freeByteArrayPool.Clear();
                                _disposedByteArrayPool.Clear();

                                for (int i = 0; i < FREE_BYTE_ARRAY_POOL_SIZE; i++)
                                {
                                    _freeByteArrayPool.Enqueue(new byte[RowBytes * Height]);
                                }
#else
                            Length = RowBytes * Height;
                            _data?.Dispose();
                            _data = new UnmanagedArray(RowBytes * Height);
#endif
                            }

                            if (!texture2D.SetSize(Width, Height, GetFormat(Format), _textureUsage))
                            {
                                throw new InvalidOperationException("Can't resize texture");
                            }
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

        

        public int RowBytes { get; private set; }

        public PixelFormat Format { get; }

        public Vector Dpi { get; set; }

        public void Dispose()
        {
            if (!Application.isExiting)
                Application.Current.Update -= OnUpdate;

#if MANAGED_BUFFER
#else
            _data?.Dispose();
#endif
            _texture.Dispose();
        }
    }
}