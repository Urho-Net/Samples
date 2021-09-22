using System;
using System.Runtime.InteropServices;

namespace Urho.Avalonia
{
    public class UnmanagedArray: IDisposable
    {
        public static readonly UnmanagedArray Empty = new UnmanagedArray();

        private IntPtr _unmanagedPointer;
        
        public UnmanagedArray(int size)
        {
            Length = size;
            _unmanagedPointer = Marshal.AllocHGlobal(size);
        }
        public UnmanagedArray()
        {
            Length = 0;
            _unmanagedPointer = IntPtr.Zero;
        }
        public int Length { get; }
        public IntPtr Addr => _unmanagedPointer;

        private void ReleaseUnmanagedResources()
        {
            if (_unmanagedPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_unmanagedPointer);
                _unmanagedPointer = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (_unmanagedPointer != IntPtr.Zero)
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }
        }

        ~UnmanagedArray()
        {
            ReleaseUnmanagedResources();
        }
    }
}