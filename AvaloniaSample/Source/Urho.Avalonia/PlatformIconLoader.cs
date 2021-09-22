using System.IO;
using Avalonia.Platform;

namespace Urho.Avalonia
{
    public class PlatformIconLoader : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream);
                return LoadIcon(stream);
            }
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new FakeIcon(stream);
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            using (var file = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                return new FakeIcon(file);
            }
        }
    }
}