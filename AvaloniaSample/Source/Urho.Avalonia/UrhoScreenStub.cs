using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Platform;

namespace Urho.Avalonia
{
    public class UrhoScreenStub : IScreenImpl, IReadOnlyList<Screen>
    {
        private readonly AvaloniaUrhoContext _context;

        public UrhoScreenStub(AvaloniaUrhoContext context)
        {
            _context = context;
            SetScreenSize(96, new PixelRect(0, 0, (int)Application.Current.Graphics.Width, (int)Application.Current.Graphics.Height));
        }

        public UrhoScreenStub(Screen screen)
        {
            Screen = screen;
        }


        public Screen Screen { get; private set; }

        int IReadOnlyCollection<Screen>.Count => 1;

        int IScreenImpl.ScreenCount => 1;

        IReadOnlyList<Screen> IScreenImpl.AllScreens => this;

        Screen IReadOnlyList<Screen>.this[int index] => Screen;

        public IEnumerator<Screen> GetEnumerator()
        {
            yield return Screen;
        }

        private void SetScreenSize(double pixelDensity, PixelRect framebufferSize)
        {
            Screen = new Screen(pixelDensity, framebufferSize, framebufferSize, true);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}