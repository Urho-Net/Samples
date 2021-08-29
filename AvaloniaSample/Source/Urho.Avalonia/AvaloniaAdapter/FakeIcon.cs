using System.IO;
using Avalonia.Platform;

namespace Urho.AvaloniaAdapter
{
    public class FakeIcon : IWindowIconImpl
    {
        private readonly Stream stream = new MemoryStream();

        public FakeIcon(Stream stream)
        {
            stream.CopyTo(this.stream);
        }

        public void Save(Stream outputStream)
        {
            stream.CopyTo(outputStream);
        }
    }
}