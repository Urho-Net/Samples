using System;
using Avalonia.Platform.Interop;

namespace Urho.Avalonia
{
    internal class DynamicLibraryLoader : IDynamicLibraryLoader
    {
        IntPtr IDynamicLibraryLoader.LoadLibrary(string dll)
        {
            throw new PlatformNotSupportedException();
        }

        IntPtr IDynamicLibraryLoader.GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            throw new PlatformNotSupportedException();
        }
    }
}